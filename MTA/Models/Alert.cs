using System.ComponentModel.DataAnnotations;

namespace MTA.Models
{
    public class Alert
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Continutul alerti este obligatoriu")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        // cheie externa (FK) - un comentariu apartine unui articol
        public int ProjectId { get; set; }

        // PASUL 6: useri si roluri 
        // cheie externa (FK) - un comentariu este postat de catre un user
        public string? UserId {  get; set; }

        // PASUL 6: useri si roluri 
        // proprietatea virtuala - un comentariu este postat de catre un user
        public virtual ApplicationUser? User { get; set; }

        // proprietatea virtuala - un comentariu apartine unui articol
        public virtual Project? Project { get; set; }
    }

}
