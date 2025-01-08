using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace MTA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<HomeController> logger

            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;

            _logger = logger;

        }

        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Projects");
            }

            var projects = db.Projects.ToList();

            //Daca exista proiecte se poate vedea primul 
            //Daca nu exista o sa se vada ce este mai jos

            if (!projects.Any())
            {
                var placeholderProject = new Project
                {
                    Id = 0,
                    Title = "No Projects Available",
                    Content = "There are currently no projects available.",
                    Date = DateTime.Now,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1),
                    DepartmentId = null,
                    UserId = null
                };

                ViewBag.FirstProject = placeholderProject;
                ViewBag.Projects = new List<Project> { placeholderProject };
            }
            else
            {
                ViewBag.FirstProject = projects.First();
                ViewBag.Projects = projects.OrderBy(o => o.Date).Skip(1).Take(2);
            }

            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}