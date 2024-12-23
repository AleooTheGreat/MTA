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
using static MTA.Models.UserProjects;


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
            // 1) Retrieve the current user's ID.
            var currentUserId = _userManager.GetUserId(User);

            // 2) Start with the full projects query (unfiltered).
            //    We'll apply role-based filtering below.
            var projects = db.Projects
                             .Include(p => p.Department)
                             .Include(p => p.User)  // the user who created the project, presumably
                             .OrderByDescending(p => p.Date);

            // If you have any TempData messages, pass them to ViewBag
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // 3) If the current user is in the "User" role, filter
            //    so that only assigned projects are shown.
            if (User.IsInRole("User"))
            {
                // --- A) Projects assigned directly to the user via UserProjects ---
                var directUserProjectIds = db.UserProjects
                                             .Where(up => up.UserId == currentUserId)
                                             .Select(up => up.ProjectId)
                                             .Where(pid => pid.HasValue) // guard if ProjectId is nullable
                                             .Select(pid => pid.Value)
                                             .Distinct()
                                             .ToList();

                // --- B) Projects assigned indirectly via Missions -> ProjectMissions ---
                // First, get all mission IDs the user is part of.
                var userMissionIds = db.UserMissions
                                       .Where(um => um.UserId == currentUserId)
                                       .Select(um => um.MissionId)
                                       .Where(mid => mid.HasValue)
                                       .Select(mid => mid.Value)
                                       .Distinct()
                                       .ToList();

                // Then get the ProjectIDs linked to those mission IDs
                var userProjectIdsFromMissions = db.ProjectMissions
                                                   .Where(pm => pm.MissionId.HasValue
                                                             && userMissionIds.Contains(pm.MissionId.Value))
                                                   .Select(pm => pm.ProjectId)
                                                   .Where(pid => pid.HasValue)
                                                   .Select(pid => pid.Value)
                                                   .Distinct()
                                                   .ToList();

                // Combine the two sets of Project IDs (direct + indirect)
                var allUserProjectIds = directUserProjectIds
                                        .Union(userProjectIdsFromMissions)
                                        .ToList();

                // Filter out only those projects that match these IDs
                projects = projects.Where(p => allUserProjectIds.Contains(p.Id))
                                   .OrderByDescending(p => p.Date);
            }

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

            // 5) Pagination
            int _perPage = 5;
            int totalItems = projects.Count();

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = 0;
            if (currentPage > 1)
            {
                offset = (currentPage - 1) * _perPage;
            }

            var paginatedProjects = projects.Skip(offset).Take(_perPage);

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Projects = paginatedProjects;

            // Base URL for pagination links
            if (!string.IsNullOrEmpty(search))
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

            Project project = db.Projects.Include(p => p.Department)
                                         .Include(p => p.User)
                                         .Include(p => p.Alerts)
                                         .ThenInclude(a => a.User)
                                         .Include(p => p.UserProjects)
                                         .ThenInclude(up => up.User)
                                         .FirstOrDefault(pr => pr.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            ViewBag.UserMissions = db.Missions
                                      .Where(m => m.UserId == _userManager.GetUserId(User))
                                      .ToList();

            ViewBag.Users = db.Users.ToList();

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
                    TempData["message"] = "This project was added in the mission!";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    
                    db.ProjectMissions.Add(projectMission);
                   
                    db.SaveChanges();

                    TempData["message"] = "The project was added to the selected mission!";
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
        public IActionResult New(Project project, IFormFile file, IFormFile? video)
        {
            var sanitizer = new HtmlSanitizer();

            project.Date = DateTime.Now;
            project.StartDate = project.StartDate == DateTime.MinValue ? DateTime.Today : project.StartDate;
            project.EndDate = project.EndDate == DateTime.MinValue ? DateTime.Today : project.EndDate;
            project.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                project.Content = sanitizer.Sanitize(project.Content);

                // First save the project so we can get the database-generated ID.
                db.Projects.Add(project);
                db.SaveChanges();

                if (file != null && file.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    project.ImagePath = "/Images/" + uniqueFileName;
                    db.SaveChanges();
                }


                if (video != null && video.Length > 0)
                {
                    var videosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Videos");
                    if (!Directory.Exists(videosFolder))
                        Directory.CreateDirectory(videosFolder);

                    var uniqueVideoName = Guid.NewGuid().ToString() + Path.GetExtension(video.FileName);
                    var videoPath = Path.Combine(videosFolder, uniqueVideoName);

                    using (var stream = new FileStream(videoPath, FileMode.Create))
                    {
                        video.CopyTo(stream);
                    }

                    project.VideoPath = "/Videos/" + uniqueVideoName;
                    db.SaveChanges();
                }

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
        public IActionResult Edit(int id, Project requestProject, IFormFile file, IFormFile? video)
        {
            var sanitizer = new HtmlSanitizer();

            Project project = db.Projects.Find(id);
            if (project == null)
            {
                TempData["message"] = "Project not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                if ((project.UserId == _userManager.GetUserId(User)) || User.IsInRole("Marshall"))
                {
                    project.Title = requestProject.Title;
                    project.Content = sanitizer.Sanitize(requestProject.Content);
                    project.StartDate = requestProject.StartDate == DateTime.MinValue ? DateTime.Today : requestProject.StartDate;
                    project.EndDate = requestProject.EndDate;
                    project.DepartmentId = requestProject.DepartmentId;
                    project.Date = DateTime.Now;
                    project.Status = requestProject.Status;

                    if (file != null && file.Length > 0)
                    {
                        try
                        {
                            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Images");
                            if (!Directory.Exists(uploadsFolder))
                                Directory.CreateDirectory(uploadsFolder);

                            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                            using (FileStream stream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(stream);
                            }


                            project.ImagePath = "/Images/" + uniqueFileName;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error uploading image: " + ex.Message);
                        }
                    }

                    if (video != null && video.Length > 0)
                    {
                        var videosFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Videos");
                        if (!Directory.Exists(videosFolder))
                            Directory.CreateDirectory(videosFolder);

                        var uniqueVideoName = Guid.NewGuid().ToString() + Path.GetExtension(video.FileName);
                        var videoPath = Path.Combine(videosFolder, uniqueVideoName);

                        using (var stream = new FileStream(videoPath, FileMode.Create))
                        {
                            video.CopyTo(stream);
                        }

                        project.VideoPath = "/Videos/" + uniqueVideoName;
                    }


                    db.SaveChanges();

                    TempData["message"] = "The project was modified!";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "You cannot modify this project as it is not yours!";
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

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult AddMember(int projectId, string userId)
        {
            var project = db.Projects.Include(p => p.UserProjects).Include(p => p.ProjectMissions).FirstOrDefault(p => p.Id == projectId);
            if (project == null)
            {
                TempData["message"] = "Project not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (project.UserId != _userManager.GetUserId(User) && !User.IsInRole("Marshall"))
            {
                TempData["message"] = "You do not have permission to add members to this project.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = projectId });
            }

            // Check if the user is part of the related mission
            var missionIds = project.ProjectMissions.Select(pm => pm.MissionId).ToList();
            var isUserInMission = db.UserMissions.Any(um => missionIds.Contains(um.MissionId) && um.UserId == userId);

            if (!isUserInMission)
            {
                TempData["message"] = "User is not part of the related mission.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = projectId });
            }

            if (db.UserProjects.Any(up => up.ProjectId == projectId && up.UserId == userId))
            {
                TempData["message"] = "User is already a member of this project.";
                TempData["messageType"] = "alert-warning";
                return RedirectToAction("Show", new { id = projectId });
            }

            var userProject = new UserProject
            {
                ProjectId = projectId,
                UserId = userId
            };

            db.UserProjects.Add(userProject);
            db.SaveChanges();

            TempData["message"] = "Member added to the project successfully.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = projectId });
        }

        [HttpPost]
        [Authorize(Roles = "Commander,Marshall")]
        public IActionResult RemoveMember(int projectId, string userId)
        {
            var project = db.Projects.Include(p => p.UserProjects).FirstOrDefault(p => p.Id == projectId);
            if (project == null)
            {
                TempData["message"] = "Project not found.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (project.UserId != _userManager.GetUserId(User) && !User.IsInRole("Marshall"))
            {
                TempData["message"] = "You do not have permission to remove members from this project.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = projectId });
            }

            var userProject = db.UserProjects.FirstOrDefault(up => up.ProjectId == projectId && up.UserId == userId);
            if (userProject != null)
            {
                db.UserProjects.Remove(userProject);
                db.SaveChanges();

                TempData["message"] = "Member removed from the project successfully.";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "User is not a member of this project.";
                TempData["messageType"] = "alert-warning";
            }

            return RedirectToAction("Show", new { id = projectId });
        }
    }
}
