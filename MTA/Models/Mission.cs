using static MTA.Models.ProjectMissions;
using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Mission
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele colectiei este obligatoriu")]
        public string Name { get; set; }

        // o colectie este creata de catre un user
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        // relatia many-to-many dintre Article si Bookmark
        public virtual ICollection<ProjectMission>? ProjectMissions { get; set; }
    }
}
