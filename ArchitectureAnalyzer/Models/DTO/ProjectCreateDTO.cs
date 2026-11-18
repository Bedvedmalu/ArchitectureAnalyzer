using System.ComponentModel.DataAnnotations;

namespace ArchitectureAnalyzer.Models.DTO
{
    public class ProjectCreateDTO
    {
        [Required(ErrorMessage = "Имя проекта обязательно для заполнения")]
        [StringLength(150, ErrorMessage = "Имя проекта не должно превышать 150 символов")]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "URL обязателен для заполнения")]
        [Url(ErrorMessage = "Введите корректный URL")]
        public string ProjectUrl { get; set; }

        public ProjectCreateDTO() { }
    }
}
