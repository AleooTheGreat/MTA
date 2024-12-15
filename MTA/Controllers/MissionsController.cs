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


        // Toti utilizatorii pot vedea Bookmark-urile existente in platforma
        // Fiecare utilizator vede bookmark-urile pe care le-a creat
        // Userii cu rolul de Admin pot sa vizualizeze toate bookmark-urile existente
        // HttpGet - implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            SetAccessRights();

            if (User.IsInRole("User") || User.IsInRole("Editor"))
            {
                var missions = from mission in db.Missions.Include("User")
                               .Where(b => b.UserId == _userManager.GetUserId(User))
                                select mission;

                ViewBag.Missions = missions;

                return View();
            }
            else 
            if(User.IsInRole("Admin"))
            {
                var missions = from mission in db.Missions.Include("User")
                                select mission;

                ViewBag.missions = missions;

                return View();
            }

            else
            {
                TempData["message"] = "Nu aveti drepturi asupra colectiei";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
            
        }

        // Afisarea tuturor articolelor pe care utilizatorul le-a salvat in 
        // bookmark-ul sau 
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            SetAccessRights();

            if (User.IsInRole("User") || User.IsInRole("Editor"))
            {
                var missions = db.Missions
                                  .Include("ProjectMissions.Project.Department")
                                  .Include("ProjectMissions.Project.User")
                                  .Include("User")
                                  .Where(b => b.Id == id)
                                  .Where(b => b.UserId == _userManager.GetUserId(User))
                                  .FirstOrDefault();
                
                if(missions == null)
                {
                    TempData["message"] = "Resursa cautata nu poate fi gasita";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Projects");
                }

                return View(missions);
            }

            else 
            if (User.IsInRole("Admin"))
            {
                var missions = db.Missions
                                  .Include("ProjectMissions.Project.Department")
                                  .Include("ProjectMissions.Project.User")
                                  .Include("User")
                                  .Where(b => b.Id == id)
                                  .FirstOrDefault();


                if (missions == null)
                {
                    TempData["message"] = "Resursa cautata nu poate fi gasita";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index", "Projects");
                }


                return View(missions);
            }

            else
            {
                TempData["message"] = "Nu aveti drepturi";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }  
        }

        // Randarea formularului in care se completeaza datele unui bookmark
        // [HttpGet] - se executa implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult New()
        {
            return View();
        }

        // Adaugarea bookmark-ului in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public ActionResult New(Mission bm)
        {
            bm.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Missions.Add(bm);
                db.SaveChanges();
                TempData["message"] = "Colectia a fost adaugata";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }

            else
            {
                return View(bm);
            }
        }


        // Conditiile de afisare a butoanelor de editare si stergere
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor") || User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }
    }
}
