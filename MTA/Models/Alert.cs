using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "The content of the alert is mandatory!")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int ProjectId { get; set; }

        public string? UserId {  get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Project? Project { get; set; }
    }

}
