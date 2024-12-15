using MTA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static MTA.Models.ProjectMissions;

namespace MTA.Data
{
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
            // Many-to-many Missions-Projects

            base.OnModelCreating(modelBuilder);

            // PK
            modelBuilder.Entity<ProjectMission>()
                .HasKey(mp => new { mp.Id, mp.ProjectId, mp.MissionId });


            // FK

            modelBuilder.Entity<ProjectMission>()
                .HasOne(mp => mp.Project)
                .WithMany(mp => mp.ProjectMissions)
                .HasForeignKey(mp => mp.ProjectId);

            modelBuilder.Entity<ProjectMission>()
                .HasOne(mp => mp.Mission)
                .WithMany(mp => mp.ProjectMissions)
                .HasForeignKey(mp => mp.MissionId);
        }
    }
}
