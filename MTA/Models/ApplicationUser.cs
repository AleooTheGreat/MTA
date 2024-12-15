using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;

// PASUL 1: useri si roluri

namespace MTA.Models
{
    public class ApplicationUser: IdentityUser
    {
        // PASUL 6: useri si roluri
        // un user poate posta mai multe comentarii
        public virtual ICollection<Alert>? Alerts { get; set; }

        // un user poate posta mai multe articole
        public virtual ICollection<Project>? Projects { get; set; }

        // un user poate sa creeze mai multe colectii
        public virtual ICollection<Mission>? Missions { get; set; }

        // atribute suplimentare adaugate pentru user
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        // variabila in care vom retine rolurile existente in baza de date
        // pentru popularea unui dropdown list
        [NotMapped]
        public IEnumerable<SelectListItem>? AllRoles { get; set; }

    }
}
