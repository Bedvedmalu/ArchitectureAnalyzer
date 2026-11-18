namespace ArchitectureAnalyzer.Models.DTO.ProjectControllerDTO
{
    public class ProjectResponseDTO
    {
        public int Id { get; set; }

        public string ProjectName { get; set; }

        public DateTime UploadDate { get; set; }

        public string ProjectLanguage { get; set; }
    }
}
