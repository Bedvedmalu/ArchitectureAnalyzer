using System.ComponentModel.DataAnnotations;

namespace ArchitectureAnalyzer.Models.DTO
{
    public class ProjectUpdateDTO
    {
        [Required(ErrorMessage = "Название проекта обязательно")]
        [StringLength(150, ErrorMessage = "Максимум 150 символов")]
        public string ProjectName { get; set; } = string.Empty;
    }
}
