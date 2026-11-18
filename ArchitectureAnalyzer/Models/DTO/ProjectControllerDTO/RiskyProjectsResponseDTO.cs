namespace ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO
{
    public class RiskyProjectsResponseDTO
    {
        public int Id { get; set; }

        public string ProjectName { get; set; }

        public int RiskyClassesCount { get; set; }
    }
}
