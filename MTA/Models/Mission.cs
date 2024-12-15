using static MTA.Models.ProjectMissions;
using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Mission
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The mission name is mandatory!")]
        public string Name { get; set; }

        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<ProjectMission>? ProjectMissions { get; set; }
    }
}
