namespace ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO
{
    public class ProjectLinesResponse
    {
        public int Id { get; set; }

        public string ProjectName { get; set; } 

        public double AvgLines { get; set; }

        public int MinLines { get; set; }

        public int MaxLines { get; set; }

        public int TotalLines { get; set; }

        public int TotalClasses { get; set; }
    }
}
