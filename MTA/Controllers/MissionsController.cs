using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static MTA.Models.UserMissions;

namespace MTA.Controllers
{
    [Authorize]
    public class MissionsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public MissionsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            SetAccessRights();

            if (User.IsInRole("Commander"))
            {
                var missions = from mission in db.Missions.Include("User")
                               .Where(b => b.UserId == _userManager.GetUserId(User))
                               select mission;

                ViewBag.Missions = missions;

                return View();
            }
            else if (User.IsInRole("Marshall"))
            {
                var missions = from mission in db.Missions.Include("User")
                               select mission;

                ViewBag.missions = missions;

                return View();
            }
            else
            {
                TempData["message"] = "You do not have access to this mission!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
        }

        [Authorize(Roles = "User,Commander,Marshall")]
        public async Task<IActionResult> Show(int id)
        {
            SetAccessRights();

            var missionQuery = db.Missions
                .Include(m => m.ProjectMissions)
                    .ThenInclude(pm => pm.Project)
                        .ThenInclude(p => p.Department)
                .Include(m => m.ProjectMissions)
                    .ThenInclude(pm => pm.Project)
                        .ThenInclude(p => p.User)
                .Include(m => m.UserMissions)
                    .ThenInclude(um => um.User);

            // Fetch the mission based on the user's role
            var userId = _userManager.GetUserId(User);
            Mission mission;

            if (User.IsInRole("Commander"))
            {
                // Commanders can only access their own missions
                mission = await missionQuery.FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            }
            else if (User.IsInRole("Marshall"))
            {
                // Marshalls can access any missions
                mission = await missionQuery.FirstOrDefaultAsync(m => m.Id == id);
            }
            else
            {
                // If the user is neither a Commander nor a Marshall
                TempData["message"] = "You do not have rights!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }

            if (mission == null)
            {
                // If no mission is found
                TempData["message"] = "The searched resource was not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }

            // Prepare a list of possible members that can be added to the mission
            var allUserIds = mission.UserMissions.Select(um => um.UserId).ToList();
            ViewBag.PossibleMembers = await _userManager.Users.Where(u => !allUserIds.Contains(u.Id)).ToListAsync();

            // Send current members to the view
            ViewBag.CurrentMembers = mission.UserMissions.Select(um => um.User).ToList();

            return View(mission);
        }




        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Commander") || User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Marshall");
            ViewBag.UserCurent = _userManager.GetUserId(User);
        }

        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public ActionResult New(Mission m)
        {
            m.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Missions.Add(m);
                db.SaveChanges();
                TempData["message"] = "The mission was added!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                return View(m);
            }
        }

        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Edit(int id)
        {
            var mission = db.Missions.Find(id);

            if (mission == null)
            {
                TempData["message"] = "The mission was not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Edit(int id, Mission mission)
        {
            if (id != mission.Id)
            {
                TempData["message"] = "Invalid mission ID!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                db.Entry(mission).State = EntityState.Modified;
                db.SaveChanges();
                TempData["message"] = "The mission was updated!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Delete(int id)
        {
            var mission = db.Missions.Find(id);

            if (mission == null)
            {
                TempData["message"] = "The mission was not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            return View(mission);
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult DeleteConfirmed(int id)
        {
            try { 
            var mission = db.Missions.Include(m => m.ProjectMissions)
                                     .Include(m => m.UserMissions)
                                     .FirstOrDefault(m => m.Id == id);

            if (mission == null)
            {
                TempData["message"] = "The mission was not found!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (mission.ProjectMissions != null && mission.ProjectMissions.Any())
            {
                TempData["message"] = "Cannot delete mission with associated projects.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Remove associated UserMissions
            if (mission.UserMissions != null && mission.UserMissions.Any())
            {
                db.UserMissions.RemoveRange(mission.UserMissions);
            }

            db.Missions.Remove(mission);

                db.SaveChanges();
                TempData["message"] = "The mission and associated users were deleted!";
                TempData["messageType"] = "alert-success";
            }
            catch (Exception ex)
            {
                TempData["message"] = "An error occurred while deleting the mission: " + ex.Message;
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public async Task<IActionResult> AddMember(int missionId, string userId)
        {
            var mission = await db.Missions
                                  .Include(m => m.UserMissions)
                                  .FirstOrDefaultAsync(m => m.Id == missionId);

            if (mission == null)
            {
                TempData["message"] = "Mission not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var userToAdd = await _userManager.FindByIdAsync(userId);
            if (userToAdd == null)
            {
                TempData["message"] = "User not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            // Check if the user is already part of the mission
            if (mission.UserMissions.Any(um => um.UserId == userId))
            {
                TempData["message"] = "User already added to the mission.";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Index");
            }

            // Authorization check: Marshall can add anyone, Commanders can add only users
            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserRoles = await _userManager.GetRolesAsync(currentUser);

            if (currentUserRoles.Contains("Commander") && !await _userManager.IsInRoleAsync(userToAdd, "Commander"))
            {
                // Commanders adding users
                mission.UserMissions.Add(new UserMission { UserId = userId, MissionId = missionId });
            }
            else if (currentUserRoles.Contains("Marshall"))
            {
                // Marshalls can add users and other commanders
                mission.UserMissions.Add(new UserMission { UserId = userId, MissionId = missionId });
            }
            else
            {
                TempData["message"] = "You do not have permission to add this member.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            await db.SaveChangesAsync();
            TempData["message"] = "Member added to mission successfully.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = missionId });
        }

        [Authorize(Roles = "Marshall")]
        public async Task<IActionResult> RemoveMember(int missionId, string userId)
        {
            var userMission = await db.UserMissions.FirstOrDefaultAsync(um => um.MissionId == missionId && um.UserId == userId);
            if (userMission != null)
            {
                db.UserMissions.Remove(userMission);
                await db.SaveChangesAsync();
                TempData["message"] = "Member removed successfully!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "Member not found!";
                TempData["messageType"] = "alert-danger";
            }

            return RedirectToAction("Show", new { id = missionId });
        }


    }
}
