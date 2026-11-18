using ArchitectureAnalyzer.Models.Entities;

namespace ArchitectureAnalyzer.Services.Interfaces
{
    public interface IParserService
    {
        Task<Project> ParseAsync(string ProjectPath);

        //Task<Project> ParseArchiveAsync(string archivePath, string ProjectName);
    }
}
