using ArchitectureAnalyzer.Data;
using ArchitectureAnalyzer.Models.DTO;
using ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO;
using ArchitectureAnalyzer.Models.Entities;
using ArchitectureAnalyzer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


//dto добавить для каждого запроса +
//убрать константы +
//контекст убрать из контроллера в сервис +
//ActionResults +

namespace ArchitectureAnalyzer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProjectsController : ControllerBase
    {
        private readonly IProjectService _ProjectService;
        public ProjectsController(IProjectService ProjectService)   
        {
            _ProjectService = ProjectService;
        }

        //GET: api/Projects
        [HttpGet]
        [Authorize]
        public async Task<IEnumerable<ProjectResponseDTO>> GetProjects()
        {
            return await _ProjectService.GetAllProjectsAsync();
        }

        //api/Projects/5
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetProject(int id)
        {
            var Project = await _ProjectService.GetProjectByIdAsync(id);
            if (Project == null)
            { 
                return NotFound();
            }
            return Ok(Project);
        }

        // POST: api/Projects
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProject(ProjectCreateDTO dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdProject =  await _ProjectService.CreateProjectAsync(dto);
            return CreatedAtAction(nameof(GetProject), new { id = createdProject.Id }, createdProject);
        }

        // PUT: api/Projects/5
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDTO dto)
        {
            var updatedProject = await _ProjectService.UpdateProjectAsync(id, dto);
            if (updatedProject == null)
            {
                return NotFound();
            }
            return Ok(updatedProject);
        }

        // DELETE: api/Projects/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProject(int id)
        {

            var isDeleted = await _ProjectService.DeleteProjectAsync(id);
            if (!isDeleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        // GET: api/Projects/risky
        [HttpGet("risky")]
        [Authorize]
        public async Task<IEnumerable<RiskyProjectsResponseDTO>> GetRiskyProjects()
        {
            return await _ProjectService.GetRiskyProjectsAsync();
        }

        // GET: api/Projects/5/health
        [HttpGet("{id}/health")]
        [Authorize]
        public async Task<IActionResult> GetProjectHealth(int id)
        {
            var health = await _ProjectService.GetProjectHealthAsync(id);
            if (health == null)
            {
                return NotFound();
            }
            return Ok(health);
        }

        // GET: api/Projects/lines
        [HttpGet("lines")]
        [Authorize]
        public async Task<IEnumerable<ProjectLinesResponse>> GetProjectLines()
        {
            return await _ProjectService.GetProjectLinesAsync();
        }
    }
}
