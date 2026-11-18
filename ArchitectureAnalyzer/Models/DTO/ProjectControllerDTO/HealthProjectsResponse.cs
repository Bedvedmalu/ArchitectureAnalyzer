namespace ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO
{
    public class HealthProjectsResponse
    {

        public string ProjectName { get; set; }

        public double HealthScore { get; set; }

        public int TotalClassesCount { get; set; }

        public int ComplexClassesCount { get; set; }
    }
}
