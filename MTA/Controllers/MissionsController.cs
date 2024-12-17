using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public IActionResult Show(int id)
        {
            SetAccessRights();

            if (User.IsInRole("Commander"))
            {
                var missions = db.Missions
                                  .Include("ProjectMissions.Project.Department")
                                  .Include("ProjectMissions.Project.User")
                                  .Include("User")
                                  .Where(m => m.Id == id)
                                  .Where(m => m.UserId == _userManager.GetUserId(User))
                                  .FirstOrDefault();

                if (missions == null)
                {
                    TempData["message"] = "The searched resource was not found!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Projects");
                }

                return View(missions);
            }
            else if (User.IsInRole("Marshall"))
            {
                var missions = db.Missions
                                  .Include("ProjectMissions.Project.Department")
                                  .Include("ProjectMissions.Project.User")
                                  .Include("User")
                                  .Where(m => m.Id == id)
                                  .FirstOrDefault();

                if (missions == null)
                {
                    TempData["message"] = "The searched resource cannot be found!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Projects");
                }

                return View(missions);
            }
            else
            {
                TempData["message"] = "You have no rights!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
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

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult DeleteConfirmed(int id)
        {
            var mission = db.Missions.Include(m => m.ProjectMissions)
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

            db.Missions.Remove(mission);
            db.SaveChanges();
            TempData["message"] = "The mission was deleted!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

    }
}
