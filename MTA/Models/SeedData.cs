using MTA.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


// PASUL 4: useri si roluri

namespace MTA.Models
{
    public class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
            serviceProvider.GetRequiredService
            <DbContextOptions<ApplicationDbContext>>()))
            {
                
                if (context.Roles.Any())
                {
                    return; 
                }


                context.Roles.AddRange(
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af-483d56fd7210", Name = "Marshall", NormalizedName = "Marshall".ToUpper() },
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af-483d56fd7211", Name = "Commander", NormalizedName = "Commander".ToUpper() },
                new IdentityRole { Id = "2c5e174e-3b0e-446f-86af-483d56fd7212", Name = "User", NormalizedName = "User".ToUpper() }
                );

               
                var hasher = new PasswordHasher<ApplicationUser>();


                context.Users.AddRange(
                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb0", 
                    UserName = "marshall@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "MARSHALL@TEST.COM",
                    Email = "marshall@test.com",
                    NormalizedUserName = "MARSHALL@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Marshall1!")
                },

                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb1",
                    UserName = "commander@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "COMMANDER@TEST.COM",
                    Email = "commander@test.com",
                    NormalizedUserName = "COMMANDER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "Commander1!")
                },

                new ApplicationUser
                {
                    Id = "8e445865-a24d-4543-a6c6-9443d048cdb2", 
                    UserName = "user@test.com",
                    EmailConfirmed = true,
                    NormalizedEmail = "USER@TEST.COM",
                    Email = "user@test.com",
                    NormalizedUserName = "USER@TEST.COM",
                    PasswordHash = hasher.HashPassword(null, "User1!")
                }
                );


                context.UserRoles.AddRange(
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7210",
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb0"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7211",
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb1"
                },
                new IdentityUserRole<string>
                {
                    RoleId = "2c5e174e-3b0e-446f-86af-483d56fd7212",
                    UserId = "8e445865-a24d-4543-a6c6-9443d048cdb2"
                }
                );

                context.SaveChanges();
            }
        }
    }
}
