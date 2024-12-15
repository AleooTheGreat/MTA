using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MTA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        // PASUL 10: useri si roluri 

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public DepartmentsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public ActionResult Index()
        {
            if (TempData.ContainsKey("message"))
            {
                ViewBag.message = TempData["message"].ToString();
            }

            var departments = from department in db.Departments
                             orderby department.DepartmentName
                             select department;
            ViewBag.Departments = departments;
            return View();
        }

        public ActionResult Show(int id)
        {
            Department department = db.Departments.Find(id);
            return View(department);
        }

        public ActionResult New()
        {
            return View();
        }

        [HttpPost]
        public ActionResult New(Department cat)
        {
            if (ModelState.IsValid)
            {
                db.Departments.Add(cat);
                db.SaveChanges();
                TempData["message"] = "Categoria a fost adaugata";
                return RedirectToAction("Index");
            }

            else
            {
                return View(cat);
            }
        }

        public ActionResult Edit(int id)
        {
            Department departemnt = db.Departments.Find(id);
            return View(departemnt);
        }

        [HttpPost]
        public ActionResult Edit(int id, Department requestDepartment)
        {
            Department department = db.Departments.Find(id);

            if (ModelState.IsValid)
            {

                department.DepartmentName = requestDepartment.DepartmentName;
                db.SaveChanges();
                TempData["message"] = "Categoria a fost modificata!";
                return RedirectToAction("Index");
            }
            else
            {
                return View(requestDepartment);
            }
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            // Category category = db.Categories.Find(id);

            Department department = db.Departments.Include("Projects")
                                             .Include("Projects.Alerts")
                                             .Where(c => c.Id == id)
                                             .First();

            db.Departments.Remove(department);

            TempData["message"] = "Categoria a fost stearsa";
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
