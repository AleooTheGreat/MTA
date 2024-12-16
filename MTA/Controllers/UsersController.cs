using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MTA.Controllers
{
    [Authorize(Roles = "Marshall")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }
        public async Task<IActionResult> Index()
        {
            var users = await db.Users
                                .OrderBy(user => user.UserName)
                                .ToListAsync();

            var userRoles = new Dictionary<string, IList<string>>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles;
            }

            ViewBag.UsersList = users;
            ViewBag.UserRoles = userRoles;

            return View();
        }


        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = db.Users.Find(id);
            var roles = await _userManager.GetRolesAsync(user);    

            ViewBag.Roles = roles;

            ViewBag.UserCurent = await _userManager.GetUserAsync(User);

            return View(user);
        }

        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            ViewBag.AllRoles = GetAllRoles();

            var roleNames = await _userManager.GetRolesAsync(user); 

            // Searching the right ID
            ViewBag.UserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .First(); 

            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            user.AllRoles = GetAllRoles();


                if (ModelState.IsValid)
                {
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.PhoneNumber = newData.PhoneNumber;
                    

                    var roles = db.Roles.ToList();

                    foreach (var role in roles)
                    {
                     
                        await _userManager.RemoveFromRoleAsync(user, role.Name);
                    }
              
                    var roleName = await _roleManager.FindByIdAsync(newRole);
                    await _userManager.AddToRoleAsync(user, roleName.ToString());

                    db.SaveChanges();
                    
                }
                return RedirectToAction("Index");
            }


        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                         .Include("Projects")
                         .Include("Alerts")
                         .Include("Missions")
                         .Where(u => u.Id == id)
                         .First();

            // Delete user alerts
            if (user.Alerts.Count > 0)
            {
                foreach (var alert in user.Alerts)
                {
                    db.Alerts.Remove(alert);
                }
            }

            // Delete user missions
            if (user.Missions.Count > 0)
            {
                foreach (var mission in user.Missions)
                {
                    db.Missions.Remove(mission);
                }
            }

            // Delete user projects
            if (user.Projects.Count > 0)
            {
                foreach (var project in user.Projects)
                {
                    db.Projects.Remove(project);
                }
            }

            db.ApplicationUsers.Remove(user);

            db.SaveChanges();

            return RedirectToAction("Index");
        }
                          

        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }
        [HttpGet]
        public IActionResult ChangeUserRole(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            ViewBag.AllRoles = GetAllRoles();

            return View(user);
        }
        [HttpPost]
        public async Task<IActionResult> ChangeUserRole(string id, [FromForm] string newRole)
        {
            ApplicationUser user = db.Users.Find(id);

            var roles = db.Roles.ToList();

            foreach (var role in roles)
            {
                await _userManager.RemoveFromRoleAsync(user, role.Name);
            }

            var roleName = await _roleManager.FindByIdAsync(newRole);
            await _userManager.AddToRoleAsync(user, roleName.ToString());

            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
