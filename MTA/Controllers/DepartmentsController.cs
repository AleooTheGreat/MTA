using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MTA.Controllers
{
    [Authorize(Roles = "Marshall")]
    public class DepartmentsController : Controller
    {

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
        public ActionResult New(Department dept)
        {
            if (ModelState.IsValid)
            {
                db.Departments.Add(dept);
                db.SaveChanges();
                TempData["message"] = "The department was added.";
                return RedirectToAction("Index");
            }

            else
            {
                return View(dept);
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
                TempData["message"] = "The department was modified!";
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
            Department department = db.Departments.Include("Projects")
                                             .Include("Projects.Alerts")
                                             .Where(d => d.Id == id)
                                             .First();

            db.Departments.Remove(department);

            TempData["message"] = "The department was deleted.";
            db.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}
