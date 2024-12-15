using System.ComponentModel.DataAnnotations.Schema;

namespace MTA.Models
{
    public class ProjectMissions
    {
    
        public class ProjectMission
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }
            public int? ProjectId { get; set; }
            public int? MissionId { get; set; }

            public virtual Project? Project { get; set; }
            public virtual Mission? Mission { get; set; }

            public DateTime MissionDate { get; set; }
        }
    }
}