namespace ArchitectureAnalyzer.Models.Entities
{
    public class Project
    {
        public int Id { get; init; }

        public string ProjectName { get; set; }

        public DateTime UploadDate { get; set; }

        public string ProjectLanguage { get; init; } = "C#";

        private List<CodeClass> _classes = new();
        private List<Dependency> _dependencies = new();

        public IReadOnlyList<CodeClass> Classes => _classes.AsReadOnly();
        public IReadOnlyList<Dependency> Dependencies => _dependencies.AsReadOnly();
        public void AddClass(CodeClass codeClass) => _classes.Add(codeClass);
        public void AddDependency(Dependency dependency) => _dependencies.Add(dependency);

        public Project(string projectName, string projectLanguage)
        {
            ProjectName = projectName;
            UploadDate = DateTime.UtcNow;
            ProjectLanguage = projectLanguage;
        }

        private Project() { }
        public double CalculateProjectHealth()
        {
            if (Classes.Count == 0) return 100;
            int complexClassesCount = Classes.Count(c => c.IsComplex());
            return Math.Max(0, 100 - complexClassesCount * 10);
        }
    }
}
