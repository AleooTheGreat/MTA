using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The department name is mandatory!")]
        public string DepartmentName { get; set; }

        public virtual ICollection<Project>? Projects { get; set; }
    }

}
