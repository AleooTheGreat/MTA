using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MTA.Controllers
{
    public class AlertsController : Controller
    {
        // Users and roles

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AlertsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        //Deleting an alert

        [HttpPost]
        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Delete(int id)
        {
            Alert alert = db.Alerts.Find(id);

            if (alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Marshall"))
            {
                db.Alerts.Remove(alert);
                db.SaveChanges();
                return Redirect("/Projects/Show/" + alert.ProjectId);
            }
            else
            {
                TempData["message"] = "You do not have the right to delete the alert";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }    
        }

       //Edit an Alert

        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Edit(int id)
        {
            Alert alert = db.Alerts.Find(id);

           if(alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Marshall"))
            {
                return View(alert);
            }
            else
            {
                TempData["message"] = "You do not have the right to edit the alert";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }            
        }

        [HttpPost]
        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Edit(int id, Alert requestAlert)
        {
            Alert alert = db.Alerts.Find(id);

            if (alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Marshall"))
            {
                if (ModelState.IsValid)
                {
                    alert.Content = requestAlert.Content;

                    db.SaveChanges();

                    return Redirect("/Projects/Show/" + alert.ProjectId);
                }
                else
                {
                    return View(requestAlert);
                }
            }
            else
            {
                TempData["message"] = "You do not have the right to edit the alert";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
        }
    }
}