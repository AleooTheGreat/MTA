using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using static MTA.Models.UserMissions;
using static MTA.Models.UserProjects;


namespace MTA.Models
{
    public class ApplicationUser: IdentityUser
    {
       
        public virtual ICollection<Alert>? Alerts { get; set; }

      
        public virtual ICollection<Project>? Projects { get; set; }

        public virtual ICollection<Mission>? Missions { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public virtual ICollection<UserMission>? UserMissions { get; set; }
        public virtual ICollection<UserProject>? UserProjects { get; set; }


        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

    }
}
