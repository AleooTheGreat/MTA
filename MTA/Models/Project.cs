using MTA.Validations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static MTA.Models.ProjectMissions;

namespace MTA.Models
{
    public class Project 
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is mandatory!")]
        [StringLength(100, ErrorMessage = "The title length should be smaller than 100 characters!")]
        [MinLength(5, ErrorMessage = "The title should have at least 5 characters!")]
        public string Title { get; set; }

        [Required(ErrorMessage = "The project content is mandatory!")]
        
        public string Content { get; set; }

        public DateTime Date { get; set; }

        [Required(ErrorMessage = "The department is mandatory!")]
        public int? DepartmentId { get; set; }

        public string? UserId { get; set; }

        public virtual Department? Department { get; set; }

 
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Alert>? Alerts { get; set; }

        public virtual ICollection<ProjectMission>? ProjectMissions { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Dept { get; set; }

    }


}
