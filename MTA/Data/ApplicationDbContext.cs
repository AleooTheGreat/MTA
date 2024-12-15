using MTA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static MTA.Models.ProjectMissions;

namespace MTA.Data
{
    // PASUL 3: useri si roluri
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<Mission> Missions { get; set; }
        public DbSet<ProjectMission> ProjectMissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // definirea relatiei many-to-many dintre Article si Bookmark

            base.OnModelCreating(modelBuilder);

            // definire primary key compus
            modelBuilder.Entity<ProjectMission>()
                .HasKey(ab => new { ab.Id, ab.ProjectId, ab.MissionId });


            // definire relatii cu modelele Bookmark si Article (FK)

            modelBuilder.Entity<ProjectMission>()
                .HasOne(ab => ab.Project)
                .WithMany(ab => ab.ProjectMissions)
                .HasForeignKey(ab => ab.ProjectId);

            modelBuilder.Entity<ProjectMission>()
                .HasOne(ab => ab.Mission)
                .WithMany(ab => ab.ProjectMissions)
                .HasForeignKey(ab => ab.MissionId);
        }
    }
}
