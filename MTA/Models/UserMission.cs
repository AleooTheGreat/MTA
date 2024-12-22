using System.ComponentModel.DataAnnotations.Schema;
namespace MTA.Models
{
    public class UserMissions
    {
        public class UserMission
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }

            public string? UserId { get; set; }
            public virtual ApplicationUser? User { get; set; }

            public int? MissionId { get; set; }
            public virtual Mission? Mission { get; set; }
        }
    }
}
