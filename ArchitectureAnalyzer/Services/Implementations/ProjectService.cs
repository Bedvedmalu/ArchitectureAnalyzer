using ArchitectureAnalyzer.Data;
using ArchitectureAnalyzer.Models.Entities;
using ArchitectureAnalyzer.Models.DTO;
using ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO;
using ArchitectureAnalyzer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace ArchitectureAnalyzer.Services.Implementations
{
    public class ProjectService: IProjectService
    {
        private readonly ArchitectureAnalyzerContext _context;
        private readonly IParserService _parserService;
        public ProjectService(ArchitectureAnalyzerContext context, IParserService parser)
        {
            _context = context;
            _parserService = parser;
        }

        public async Task<IEnumerable<ProjectResponseDTO>> GetAllProjectsAsync()
        {
             return await _context.Project
                .Select(p => new ProjectResponseDTO
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,
                    UploadDate = p.UploadDate,
                    ProjectLanguage = p.ProjectLanguage
                }).ToListAsync();
        }

        public async Task<ProjectResponseDTO?> GetProjectByIdAsync(int id)
        {
            var Project = await _context.Project
                .FirstOrDefaultAsync(p => p.Id == id);
            if (Project == null)
            {
                return null;
            }
            return new ProjectResponseDTO
            {
                Id = Project.Id,
                ProjectName = Project.ProjectName,
                UploadDate = Project.UploadDate,
                ProjectLanguage = Project.ProjectLanguage
            };
        }
     
        public async Task<ProjectResponseDTO?> CreateProjectAsync(ProjectCreateDTO dto)
        {
            var Project = await _parserService.ParseAsync(dto.ProjectUrl);
            Project.ProjectName = dto.ProjectName;
            _context.Project.Add(Project);
            await _context.SaveChangesAsync();

            return new ProjectResponseDTO
            {
                Id = Project.Id,
                ProjectName = Project.ProjectName,
                UploadDate = Project.UploadDate,
                ProjectLanguage = Project.ProjectLanguage
            };
        }

        public async Task<ProjectResponseDTO?> UpdateProjectAsync(int id, ProjectUpdateDTO dto)
        {
            var Project = await _context.Project.FindAsync(id);
            if (Project == null)
            {
                return null;
            }
            Project.ProjectName = dto.ProjectName;
            await _context.SaveChangesAsync();

            return new ProjectResponseDTO
            {
                Id = Project.Id,
                ProjectName = Project.ProjectName,
                UploadDate = Project.UploadDate,
                ProjectLanguage = Project.ProjectLanguage
            };
        }

        public async Task<bool> DeleteProjectAsync(int id)
        {
            var Project = await _context.Project.FindAsync(id);
            if (Project == null)
            {
                return false;
            }
            _context.Project.Remove(Project);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<RiskyProjectsResponseDTO>> GetRiskyProjectsAsync()
        {
            return await _context.Project
                .Where(p => p.Classes.Any(c => c.CyclomaticComplexity > Constants.RequiredCyclomaticComplexity))
                .Select(p => new RiskyProjectsResponseDTO
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,
                    RiskyClassesCount = p.Classes.Count(c => c.CyclomaticComplexity > Constants.RequiredCyclomaticComplexity)
                })
                .ToListAsync();
        }

        public async Task<HealthProjectsResponse?> GetProjectHealthAsync(int id)
        {
            var Project = await _context.Project
                .Include(p => p.Classes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (Project == null)
            {
                return null;
            }
            return new HealthProjectsResponse
            {
                ProjectName = Project.ProjectName,
                HealthScore = Project.CalculateProjectHealth(),
                TotalClassesCount = Project.Classes.Count,
                ComplexClassesCount = Project.Classes.Count(c => c.IsComplex())
            };
        }

        public async Task<IEnumerable<ProjectLinesResponse>> GetProjectLinesAsync()
        {
            return await _context.Project
                .Select(p => new ProjectLinesResponse
                {
                    Id = p.Id,
                    ProjectName = p.ProjectName,
                    AvgLines = p.Classes.Select(c => (double?)c.LinesCount).Average() ?? 0,
                    MinLines = p.Classes.Select(c => (int?)c.LinesCount).Min() ?? 0,
                    MaxLines = p.Classes.Select(c => (int?)c.LinesCount).Max() ?? 0,
                    TotalLines = p.Classes.Sum(c => c.LinesCount),
                    TotalClasses = p.Classes.Count
                }).ToListAsync();
        }
    }
}
