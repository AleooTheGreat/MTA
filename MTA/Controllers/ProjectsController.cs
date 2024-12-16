using MTA.Data;
using MTA.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using static MTA.Models.ProjectMissions;
using Ganss.Xss;
using TaskStatus = MTA.Models.TaskStatus;


namespace MTA.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ProjectsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        
        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Index()
        {
            var projects = db.Projects.Include("Department")
                                      .Include("User")
                                      .OrderByDescending(p => p.Date);


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            //Basic search engine

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim();// If you try to be funny you can't ;)

                
                List<int> projectIds = db.Projects.Where
                                        (
                                         pj => pj.Title.Contains(search)
                                         || pj.Content.Contains(search)
                                        ).Select(p => p.Id).ToList();

             
                List<int> projectIdsOfAlertsWithSearchString = db.Alerts
                                        .Where
                                        (
                                         c => c.Content.Contains(search)
                                        ).Select(c => (int)c.ProjectId).ToList();

                List<int> mergedIds = projectIds.Union(projectIdsOfAlertsWithSearchString).ToList();


                projects = db.Projects.Where(project => mergedIds.Contains(project.Id))
                                      .Include("Department")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

            }

            ViewBag.SearchString = search;

            int _perPage = 5;

            int totalItems = projects.Count();

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            var offset = 0;

            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            var paginatedProjects = projects.Skip(offset).Take(_perPage);


            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            ViewBag.Projects = paginatedProjects;

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Projects/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Projects/Index/?page";
            }

            return View();
        }

       
        public Dictionary<int, int> GetProjectAlertCounts()
        {
            var projectCommentCounts = db.Projects
                                         .Include(p => p.Alerts)
                                         .ToDictionary(p => p.Id, p => p.Alerts.Count);
            return projectCommentCounts;
        }

        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Show(int id)
        {
            var initialAlertCounts = GetProjectAlertCounts();

            /*
             * Debug like a noob :) 
             * foreach (Project proj in db.Projects)
            {
                Console.WriteLine($"Project ID: {proj.Id}");
            }
            Console.WriteLine($"Project ID Cautat: {id}");*/
           
            Project project = db.Projects.Include("Department")
                                         .Include("User")
                                         .Include("Alerts")
                                         .Include("Alerts.User")
                              .Where(pr => pr.Id == id)
                              .First();

            ViewBag.UserMissions = db.Missions
                                      .Where(m => m.UserId == _userManager.GetUserId(User))
                                      .ToList();

            SetAccessRights();

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            ViewBag.InitialCommentCounts = initialAlertCounts;

            return View(project);
        }

        [HttpPost]
        [Authorize(Roles = "User,Commander,Marshall")]
        public IActionResult Show([FromForm] Alert alert)
        {
            var initialAlertCounts = GetProjectAlertCounts();

            alert.Date = DateTime.Now;

            alert.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                if (alert.ProjectId == 0)
                {
                    TempData["message"] = "Invalid project ID.";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }

                db.Alerts.Add(alert);
                db.SaveChanges();

                var updatedAlertCounts = GetProjectAlertCounts();

                var projectIdWithDifferentAlertCount = updatedAlertCounts
                    .FirstOrDefault(kvp => kvp.Value != initialAlertCounts[kvp.Key]).Key;

                return Redirect("/Projects/Show/" + projectIdWithDifferentAlertCount);
            }
            else
            {
                Project pr = db.Projects.Include("Department")
                                         .Include("User")
                                         .Include("Alerts")
                                         .Include("Alerts.User")
                                         .Where(pr => pr.Id == alert.ProjectId)
                                         .First();

                ViewBag.UserMissions = db.Missions
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                SetAccessRights();

                return View(pr);
            }
        }


        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult AddMission([FromForm] ProjectMission projectMission)
        {
            
            if (ModelState.IsValid)
            {
                if (db.ProjectMissions
                    .Where(pm => pm.ProjectId == projectMission.ProjectId)
                    .Where(pm => pm.MissionId == projectMission.MissionId)
                    .Count() > 0)
                {
                    TempData["message"] = "This project was already added in the mission!";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    
                    db.ProjectMissions.Add(projectMission);
                   
                    db.SaveChanges();

                    TempData["message"] = "The project was already added to the selected mission!";
                    TempData["messageType"] = "alert-success";
                }

            }
            else
            {
                TempData["message"] = "The project could not be added to the mission!";
                TempData["messageType"] = "alert-danger";
            }

            return Redirect("/Projects/Show/" + projectMission.ProjectId);
        }


        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult New()
        {
            Project project = new Project();

            project.StartDate = DateTime.Today;
            project.EndDate = DateTime.Today;
            project.Dept = GetAllDepartments();

            return View(project);
        }

        
        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult New(Project project)
        {
            var sanitizer = new HtmlSanitizer();

            project.Date = DateTime.Now;
            project.StartDate = project.StartDate == DateTime.MinValue ? DateTime.Today : project.StartDate;
            project.EndDate = project.EndDate == DateTime.MinValue ? DateTime.Today : project.EndDate;

            project.UserId = _userManager.GetUserId(User);

            if(ModelState.IsValid)
            {
                project.Content = sanitizer.Sanitize(project.Content);

                db.Projects.Add(project);
                db.SaveChanges();
                TempData["message"] = "The project was added!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                project.Dept = GetAllDepartments();
                return View(project);
            }
        }

        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Edit(int id)
        {

            Project project = db.Projects.Include("Department")
                                         .Where(pr => pr.Id == id)
                                         .First();

            if (project.StartDate == DateTime.MinValue)
            {
                project.StartDate = DateTime.Today;
            }

            project.Dept = GetAllDepartments();

            if ((project.UserId == _userManager.GetUserId(User)) || 
                User.IsInRole("Marshall"))
            {
                return View(project);
            }
            else
            {    
                
                TempData["message"] = "You can not modify an project that is not yours!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }  
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult Edit(int id, Project requestProject)
        {
            var sanitizer = new HtmlSanitizer();

            Project project = db.Projects.Find(id);

            if(ModelState.IsValid)
            {
                if((project.UserId == _userManager.GetUserId(User)) 
                    || User.IsInRole("Marshall"))
                {
                    project.Title = requestProject.Title;

                    requestProject.Content = sanitizer.Sanitize(requestProject.Content);

                    project.Content = requestProject.Content;

                    project.StartDate = requestProject.StartDate == DateTime.MinValue ? DateTime.Today : requestProject.StartDate; 
                    project.EndDate = requestProject.EndDate;

                    project.DepartmentId = requestProject.DepartmentId;
                    project.Date = DateTime.Now;

                    TempData["message"] = "The project was modified!";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {                    
                    TempData["message"] = "You can not modify this project as it is not your!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestProject.Dept = GetAllDepartments();
                return View(requestProject);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public ActionResult Delete(int id)
        {

            Project project = db.Projects.Include("Alerts")
                                         .Where(pr => pr.Id == id)
                                         .First();

            if ((project.UserId == _userManager.GetUserId(User))
                    || User.IsInRole("Marshall"))
            {
                db.Projects.Remove(project);
                db.SaveChanges();
                TempData["message"] = "The project was deleted";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "You can not delete this project as it is not yours.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }    
        }

       
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Commander"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Marshall");
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllDepartments()
        {
            var selectList = new List<SelectListItem>();

            var departments = from dep in db.Departments
                              select dep;

            foreach (var department in departments)
            {
                selectList.Add(new SelectListItem
                {
                    Value = department.Id.ToString(),
                    Text = department.DepartmentName
                });
            }
           
            return selectList;
        }

        [HttpPost]
        public IActionResult UploadImage(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                try
                {
                    // Define the upload path (wwwroot/Images)
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    // Generate a unique file name
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file to the server
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    // Generate the URL to return
                    var imageUrl = Url.Content("~/Images/" + uniqueFileName);
                    return Json(imageUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving file: " + ex.Message);
                    return BadRequest("Image upload failed.");
                }
            }

            return BadRequest("Invalid file.");
        }

        [HttpPost]
        [Authorize(Roles = "User,Commander,Marshall")]
        public async Task<IActionResult> ChangeStatus(int id, TaskStatus status)
        {
            var project = await db.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }

            project.Status = status;
            await db.SaveChangesAsync();
            Console.WriteLine("Url: " + Url.Action("Show", new { id = id }));
            return RedirectToAction("Show", new { id = project.Id });
        }


        public IActionResult IndexNou()
        {
            return View();
        }
    }
}
