using System.ComponentModel.DataAnnotations.Schema;

namespace MTA.Models
{
    public class ProjectMissions
    {
        // tabelul asociativ care face legatura intre Article si Bookmark
        // un articol are mai multe colectii din care face parte
        // iar o colectie contine mai multe articole in cadrul ei
        public class ProjectMission
        {
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            // cheie primara compusa (Id, ArticleId, BookmarkId)
            public int Id { get; set; }
            public int? ProjectId { get; set; }
            public int? MissionId { get; set; }

            public virtual Project? Project { get; set; }
            public virtual Mission? Mission { get; set; }

            public DateTime MissionDate { get; set; }
        }
    }
}