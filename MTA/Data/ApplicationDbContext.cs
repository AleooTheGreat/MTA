using MTA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using static MTA.Models.ProjectMissions;
using static MTA.Models.UserMissions;
using static MTA.Models.UserProjects;

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

        public DbSet<UserProject> UserProjects { get; set; }

        public DbSet<UserMission> UserMissions { get; set; }

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

            modelBuilder.Entity<UserMission>()
                .HasOne(um => um.User)
                .WithMany(u => u.UserMissions)
                .HasForeignKey(um => um.UserId);

            modelBuilder.Entity<UserMission>()
                .HasOne(um => um.Mission)
                .WithMany(m => m.UserMissions)
                .HasForeignKey(um => um.MissionId);

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.User)
                .WithMany(u => u.UserProjects)
                .HasForeignKey(up => up.UserId);

            modelBuilder.Entity<UserProject>()
                .HasOne(up => up.Project)
                .WithMany(p => p.UserProjects)
                .HasForeignKey(up => up.ProjectId);
        }
    }
}
