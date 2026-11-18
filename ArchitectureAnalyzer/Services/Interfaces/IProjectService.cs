using ArchitectureAnalyzer.Models.DTO;
using ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO;


public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDTO>> GetAllProjectsAsync();
    Task<ProjectResponseDTO?> GetProjectByIdAsync(int id);
    Task<ProjectResponseDTO?> CreateProjectAsync(ProjectCreateDTO dto);
    //Task<ProjectResponseDTO?> CreateProjectFromArchiveAsync(IFormFile file, string ProjectName);
    Task<ProjectResponseDTO?> UpdateProjectAsync(int id, ProjectUpdateDTO dto);
    Task<bool> DeleteProjectAsync(int id);
    Task<IEnumerable<RiskyProjectsResponseDTO>> GetRiskyProjectsAsync();
    Task<HealthProjectsResponse?> GetProjectHealthAsync(int id);
    Task<IEnumerable<ProjectLinesResponse>> GetProjectLinesAsync();
}