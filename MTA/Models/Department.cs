using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele categoriei este obligatoriu")]
        public string DepartmentName { get; set; }

        // proprietatea virtuala - dintr-o categorie fac parte mai multe articole
        public virtual ICollection<Project>? Projects { get; set; }
    }

}
