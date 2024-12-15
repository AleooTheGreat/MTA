using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MTA.Controllers
{
    public class AlertsController : Controller
    {
        // PASUL 10: useri si roluri 

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
        /*
        
        // Adaugarea unui comentariu asociat unui articol in baza de date
        [HttpPost]
        public IActionResult New(Comment comm)
        {
            comm.Date = DateTime.Now;

            if(ModelState.IsValid)
            {
                db.Comments.Add(comm);
                db.SaveChanges();
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }

            else
            {
                return Redirect("/Articles/Show/" + comm.ArticleId);
            }

        }

        
        */


        // Stergerea unui comentariu asociat unui articol din baza de date
        // Se poate sterge comentariul doar de catre userii cu rolul de Admin 
        // sau de catre utilizatorii cu rolul de User sau Editor, doar daca 
        // acel comentariu a fost postat de catre acestia

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Delete(int id)
        {
            Alert alert = db.Alerts.Find(id);

            if (alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                db.Alerts.Remove(alert);
                db.SaveChanges();
                return Redirect("/Projects/Show/" + alert.ProjectId);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti alerta";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }    
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un comentariu existent
        // Editarea unui comentariu asociat unui articol
        // [HttpGet] - se executa implicit
        // Se poate edita un comentariu doar de catre utilizatorul care a postat comentariul respectiv 
        // Adminii pot edita orice comentariu, chiar daca nu a fost postat de ei

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id)
        {
            Alert alert = db.Alerts.Find(id);

           if(alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
            {
                return View(alert);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa editati alerta";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }            
        }

        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Edit(int id, Alert requestAlert)
        {
            Alert alert = db.Alerts.Find(id);

            if (alert.UserId == _userManager.GetUserId(User) || User.IsInRole("Admin"))
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
                TempData["message"] = "Nu aveti dreptul sa editati alerta";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Projects");
            }
        }
    }
}