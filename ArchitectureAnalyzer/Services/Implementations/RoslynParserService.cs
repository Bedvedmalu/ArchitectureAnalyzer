using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using ArchitectureAnalyzer.Models.Entities;
using ArchitectureAnalyzer.Services.Interfaces;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using MyProject = ArchitectureAnalyzer.Models.Entities.Project;

namespace ArchitectureAnalyzer.Services.Implementations;

public class RoslynParserService : IParserService
{
    private static readonly object RegistrationLock = new();
    private readonly HttpClient _httpClient;

    public RoslynParserService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<MyProject> ParseAsync(string ProjectPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ProjectPath);

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "ArchAnalyzer",
            Guid.NewGuid().ToString("N"));

        try
        {
            var csproj = await ResolveProjectPathAsync(
                ProjectPath, tempDirectory);

            RegisterMSBuild();
            await RestoreAsync(csproj);

            return await AnalyzeAsync(csproj);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static void RegisterMSBuild()
    {
        lock (RegistrationLock)
        {
            if (!MSBuildLocator.IsRegistered)
                MSBuildLocator.RegisterDefaults();
        }
    }

    // Создание workspace вынесено из метода регистрации MSBuild.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<MyProject> AnalyzeAsync(string csproj)
    {
        using var workspace = MSBuildWorkspace.Create();
        var project = await workspace.OpenProjectAsync(csproj);

        var failures = workspace.Diagnostics
            .Where(d => d.Kind == WorkspaceDiagnosticKind.Failure)
            .ToArray();

        if (failures.Length > 0)
        {
            throw new InvalidOperationException(
                "Не удалось загрузить проект Roslyn: " +
                string.Join(
                    Environment.NewLine,
                    failures.Select(d => d.Message)));
        }

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                "Roslyn не смог создать компиляцию проекта.");

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Take(10)
            .ToArray();

        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                "Исправьте ошибки компиляции анализируемого проекта: " +
                string.Join(
                    Environment.NewLine,
                    errors.Select(d => d.ToString())));
        }

        var result = new MyProject(project.Name, "C#");

        var classes = new Dictionary<ISymbol, CodeClass>(
            SymbolEqualityComparer.Default);

        var declarations = new List<(
            ClassDeclarationSyntax Syntax,
            SemanticModel Model,
            INamedTypeSymbol Symbol)>();

        // Первый проход: классы выбранного проекта.
        foreach (var document in project.Documents)
        {
            var tree = await document.GetSyntaxTreeAsync();
            if (tree == null)
                continue;

            var model = compilation.GetSemanticModel(tree);
            var root = await tree.GetRootAsync();

            foreach (var declaration in root.DescendantNodes()
                         .OfType<ClassDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(declaration)
                    is not INamedTypeSymbol symbol)
                {
                    continue;
                }

                declarations.Add((declaration, model, symbol));

                // Все части partial-класса соответствуют одному символу.
                if (!classes.TryGetValue(symbol, out var codeClass))
                {
                    codeClass = new CodeClass(
                        0,
                        symbol.Name,
                        symbol.ContainingNamespace.IsGlobalNamespace
                            ? ""
                            : symbol.ContainingNamespace.ToDisplayString(),
                        1,
                        0);

                    classes.Add(symbol, codeClass);
                    result.AddClass(codeClass);
                }

                codeClass.CyclomaticComplexity += CountBranches(declaration);
                codeClass.LinesCount += CountLines(declaration);
            }
        }

        var seen = new HashSet<(
            CodeClass Source,
            CodeClass Target,
            string Type)>();

        void AddDependency(
            INamedTypeSymbol source,
            ITypeSymbol? target,
            string type)
        {
            if (target == null)
                return;

            if (target is IArrayTypeSymbol array)
            {
                AddDependency(source, array.ElementType, type);
                return;
            }

            if (target is not INamedTypeSymbol named)
                return;

            // Box<Target> сопоставляется с объявлением Box<T>.
            if (classes.TryGetValue(source.OriginalDefinition, out var from)
                && classes.TryGetValue(named.OriginalDefinition, out var to)
                && from != to
                && seen.Add((from, to, type)))
            {
                result.AddDependency(new Dependency(from, to, type));
            }

            // Например, List<Target> создаёт связь с Target.
            foreach (var argument in named.TypeArguments)
                AddDependency(source, argument, type);
        }

        // Второй проход: зависимости между найденными классами.
        foreach (var (syntax, model, symbol) in declarations)
        {
            if (syntax.BaseList != null)
            {
                foreach (var baseType in syntax.BaseList.Types)
                {
                    AddDependency(
                        symbol,
                        model.GetTypeInfo(baseType.Type).Type,
                        "Inheritance");
                }
            }

            foreach (var member in syntax.Members)
            {
                var typeSyntax = member switch
                {
                    FieldDeclarationSyntax field =>
                        field.Declaration.Type,
                    PropertyDeclarationSyntax property =>
                        property.Type,
                    _ => null
                };

                if (typeSyntax != null)
                {
                    AddDependency(
                        symbol,
                        model.GetTypeInfo(typeSyntax).Type,
                        "Aggregation");
                }
            }

            foreach (var node in OwnedNodes(syntax))
            {
                if (node is InvocationExpressionSyntax invocation
                    && model.GetSymbolInfo(invocation).Symbol
                        is IMethodSymbol method)
                {
                    AddDependency(
                        symbol, method.ContainingType, "Usage");
                }
                else if (node is BaseObjectCreationExpressionSyntax creation)
                {
                    AddDependency(
                        symbol,
                        model.GetTypeInfo(creation).Type,
                        "Usage");
                }
            }
        }

        return result;
    }

    private static IEnumerable<SyntaxNode> OwnedNodes(
        ClassDeclarationSyntax declaration)
    {
        // Тело вложенного типа анализируется отдельно.
        return declaration.DescendantNodes(node =>
            node == declaration
            || node is not BaseTypeDeclarationSyntax
                && node is not DelegateDeclarationSyntax);
    }

    private static int CountBranches(ClassDeclarationSyntax declaration)
    {
        return OwnedNodes(declaration).Count(node =>
            node is IfStatementSyntax
                or ForStatementSyntax
                or ForEachStatementSyntax
                or ForEachVariableStatementSyntax
                or WhileStatementSyntax
                or DoStatementSyntax
                or CaseSwitchLabelSyntax
                or CasePatternSwitchLabelSyntax
                or CatchClauseSyntax
                or ConditionalExpressionSyntax
            || node is SwitchExpressionArmSyntax arm
                && arm.Pattern is not DiscardPatternSyntax
            || node is BinaryExpressionSyntax binary
                && (binary.IsKind(SyntaxKind.LogicalAndExpression)
                    || binary.IsKind(SyntaxKind.LogicalOrExpression)
                    || binary.IsKind(SyntaxKind.CoalesceExpression)));
    }

    private static int CountLines(ClassDeclarationSyntax declaration)
    {
        var text = declaration.SyntaxTree.GetText();
        var lines = text.Lines.GetLinePositionSpan(declaration.Span);
        var count = lines.End.Line - lines.Start.Line + 1;

        // Исключаем строки, полностью занятые вложенными типами.
        var excluded = new HashSet<int>();

        var nestedTypes = declaration.DescendantNodes(node =>
                node == declaration
                || node is not BaseTypeDeclarationSyntax
                    && node is not DelegateDeclarationSyntax)
            .Where(node =>
                node is BaseTypeDeclarationSyntax
                    or DelegateDeclarationSyntax);

        foreach (var nested in nestedTypes)
        {
            var span = text.Lines.GetLinePositionSpan(nested.Span);

            for (var line = span.Start.Line; line <= span.End.Line; line++)
            {
                var sourceLine = text.Lines[line];

                var before = text.ToString(
                    Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(
                        sourceLine.Start,
                        Math.Max(sourceLine.Start, nested.SpanStart)));

                var after = text.ToString(
                    Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(
                        Math.Min(sourceLine.End, nested.Span.End),
                        sourceLine.End));

                if (string.IsNullOrWhiteSpace(before)
                    && string.IsNullOrWhiteSpace(after))
                {
                    excluded.Add(line);
                }
            }
        }

        return count - excluded.Count;
    }

    private async Task<string> ResolveProjectPathAsync(
        string input,
        string tempDirectory)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            if (uri.Host.Equals(
                    "github.com",
                    StringComparison.OrdinalIgnoreCase))
            {
                var parts = uri.AbsolutePath.Trim('/').Split('/');

                if (parts.Length == 2)
                {
                    var repository = parts[1].EndsWith(
                        ".git", StringComparison.OrdinalIgnoreCase)
                        ? parts[1][..^4]
                        : parts[1];

                    // HEAD указывает на ветку репозитория по умолчанию.
                    uri = new Uri(
                        $"https://github.com/{parts[0]}/{repository}/archive/HEAD.zip");
                }
            }

            if (!uri.AbsolutePath.EndsWith(
                    ".zip", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Укажите URL GitHub-репозитория или ZIP-архива.");
            }

            Directory.CreateDirectory(tempDirectory);
            var archivePath = Path.Combine(tempDirectory, "download.zip");

            using (var response = await _httpClient.GetAsync(
                       uri, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                await using var stream = File.Create(archivePath);
                await response.Content.CopyToAsync(stream);
            }

            var sourceDirectory = Path.Combine(tempDirectory, "source");
            ZipFile.ExtractToDirectory(archivePath, sourceDirectory);

            return FindProject(sourceDirectory);
        }

        var path = Path.GetFullPath(input);

        if (File.Exists(path)
            && Path.GetExtension(path).Equals(
                ".csproj", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (File.Exists(path)
            && Path.GetExtension(path).Equals(
                ".zip", StringComparison.OrdinalIgnoreCase))
        {
            var sourceDirectory = Path.Combine(tempDirectory, "source");
            Directory.CreateDirectory(sourceDirectory);

            ZipFile.ExtractToDirectory(path, sourceDirectory);
            return FindProject(sourceDirectory);
        }

        if (Directory.Exists(path))
            return FindProject(path);

        throw new ArgumentException(
            $"Не найден .csproj, каталог или ZIP-архив: {input}");
    }

    private static string FindProject(string directory)
    {
        var projects = Directory
            .EnumerateFiles(directory, "*.csproj", SearchOption.AllDirectories)
            .Where(path =>
                !Path.GetRelativePath(directory, path)
                    .Split(Path.DirectorySeparatorChar)
                    .Any(part =>
                        part.Equals("bin", StringComparison.OrdinalIgnoreCase)
                        || part.Equals("obj", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        return projects.Length switch
        {
            0 => throw new InvalidOperationException(
                "В источнике не найден .csproj файл."),

            1 => projects[0],

            _ => throw new InvalidOperationException(
                "Найдено несколько .csproj. Передайте локальный путь " +
                "к конкретному проекту или архив с одним проектом: " +
                string.Join(", ", projects.Select(path =>
                    Path.GetRelativePath(directory, path))))
        };
    }

    private static async Task RestoreAsync(string csproj)
    {
        // MSBuildWorkspace самостоятельно не выполняет NuGet restore.
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(csproj)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("restore");
        startInfo.ArgumentList.Add(csproj);
        startInfo.ArgumentList.Add("--nologo");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException(
                "Не удалось запустить dotnet restore. Установите .NET SDK.");

        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(
            TimeSpan.FromMinutes(3));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();

            throw new TimeoutException(
                "dotnet restore не завершился за 3 минуты.");
        }

        var log = await output + Environment.NewLine + await error;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Не удалось восстановить зависимости проекта: " + log);
        }
    }
}