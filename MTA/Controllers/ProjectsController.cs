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
using static MTA.Models.ProjectMissions;


namespace MTA.Controllers
{
    [Authorize]
    public class ProjectsController : Controller
    {
        // PASUL 10: useri si roluri 

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

        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte
        // Pentru fiecare articol se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] care se executa implicit
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Index()
        {
            var projects = db.Projects.Include("Department")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

            // ViewBag.OriceDenumireSugestiva
            // ViewBag.Articles = articles;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // MOTOR DE CAUTARE

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in articol (Title si Content)
                
                List<int> projectIds = db.Projects.Where
                                        (
                                         at => at.Title.Contains(search)
                                         || at.Content.Contains(search)
                                        ).Select(a => a.Id).ToList();

                // Cautare in comentarii (Content)
                List<int> projectIdsOfAlertsWithSearchString = db.Alerts
                                        .Where
                                        (
                                         c => c.Content.Contains(search)
                                        ).Select(c => (int)c.ProjectId).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = projectIds.Union(projectIdsOfAlertsWithSearchString).ToList();


                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                projects = db.Projects.Where(project => mergedIds.Contains(project.Id))
                                      .Include("Department")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 articole pe pagina
            int _perPage = 3;

            // Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            // metoda Count()

            int totalItems = projects.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedProjects = projects.Skip(offset).Take(_perPage);


            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Projects = paginatedProjects;

            // DACA AVEM AFISAREA PAGINATA IMPREUNA CU SEARCH

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

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // Se afiseaza si userul care a postat articolul respectiv
        // [HttpGet] se executa implicit implicit
        public Dictionary<int, int> GetProjectCommentCounts()
        {
            var projectCommentCounts = db.Projects
                                         .Include(p => p.Alerts)
                                         .ToDictionary(p => p.Id, p => p.Alerts.Count);
            return projectCommentCounts;
        }

        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show(int id)
        {
            var initialCommentCounts = GetProjectCommentCounts();

            foreach (Project proj in db.Projects)
            {
                Console.WriteLine($"Project ID: {proj.Id}");
            }
            Console.WriteLine($"Project ID Cautat: {id}");
            /*
            if (id == 0)
            {
                id = db.Projects.Select(p => p.Id).FirstOrDefault(); // Pick the first project ID
                Console.WriteLine($"Project ID Modificat: {id}");
            }
            */
            Project project = db.Projects.Include("Department")
                                         .Include("User")
                                         .Include("Alerts")
                                         .Include("Alerts.User")
                              .Where(pr => pr.Id == id)
                              .First();

            // Adaugam bookmark-urile utilizatorului pentru dropdown
            ViewBag.UserMissions = db.Missions
                                      .Where(b => b.UserId == _userManager.GetUserId(User))
                                      .ToList();

            SetAccessRights();

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            ViewBag.InitialCommentCounts = initialCommentCounts;

            return View(project);
        }


        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // HttpGet implicit


        // Adaugarea unui comentariu asociat unui articol in baza de date
        // Toate rolurile pot adauga comentarii in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Alert alert)
        {
            var initialCommentCounts = GetProjectCommentCounts();

            alert.Date = DateTime.Now;

            // preluam Id-ul utilizatorului care posteaza comentariul
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

                var updatedCommentCounts = GetProjectCommentCounts();

                // Find the project with a different comment count
                var projectIdWithDifferentCommentCount = updatedCommentCounts
                    .FirstOrDefault(kvp => kvp.Value != initialCommentCounts[kvp.Key]).Key;

                return Redirect("/Projects/Show/" + projectIdWithDifferentCommentCount);
            }
            else
            {
                Project pr = db.Projects.Include("Department")
                                         .Include("User")
                                         .Include("Alerts")
                                         .Include("Alerts.User")
                                         .Where(pr => pr.Id == alert.ProjectId)
                                         .First();

                // Adaugam bookmark-urile utilizatorului pentru dropdown
                ViewBag.UserMissions = db.Missions
                                          .Where(b => b.UserId == _userManager.GetUserId(User))
                                          .ToList();

                SetAccessRights();

                return View(pr);
            }
        }


        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult AddMission([FromForm] ProjectMission projectMission)
        {
            // Daca modelul este valid
            if (ModelState.IsValid)
            {
                // Verificam daca avem deja articolul in colectie
                if (db.ProjectMissions
                    .Where(ab => ab.ProjectId == projectMission.ProjectId)
                    .Where(ab => ab.MissionId == projectMission.MissionId)
                    .Count() > 0)
                {
                    TempData["message"] = "Acest articol este deja adaugat in colectie";
                    TempData["messageType"] = "alert-danger";
                }
                else
                {
                    // Adaugam asocierea intre articol si bookmark 
                    db.ProjectMissions.Add(projectMission);
                    // Salvam modificarile
                    db.SaveChanges();

                    // Adaugam un mesaj de succes
                    TempData["message"] = "Articolul a fost adaugat in colectia selectata";
                    TempData["messageType"] = "alert-success";
                }

            }
            else
            {
                TempData["message"] = "Nu s-a putut adauga articolul in colectie";
                TempData["messageType"] = "alert-danger";
            }

            // Ne intoarcem la pagina articolului
            return Redirect("/Projects/Show/" + projectMission.ProjectId);
        }


        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // Doar utilizatorii cu rolul de Editor si Admin pot adauga articole in platforma
        // [HttpGet] - care se executa implicit

        [Authorize(Roles = "Editor,Admin")]
        public IActionResult New()
        {
            Project project = new Project();

            project.Categ = GetAllDepartments();

            return View(project);
        }

        // Se adauga articolul in baza de date
        // Doar utilizatorii cu rolul Editor si Admin pot adauga articole in platforma
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult New(Project project)
        {
            var sanitizer = new HtmlSanitizer();

            project.Date = DateTime.Now;

            // preluam Id-ul utilizatorului care posteaza articolul
            project.UserId = _userManager.GetUserId(User);

            if(ModelState.IsValid)
            {
                project.Content = sanitizer.Sanitize(project.Content);

                db.Projects.Add(project);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost adaugat";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                project.Categ = GetAllDepartments();
                return View(project);
            }
        }

        // Se editeaza un articol existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // Se afiseaza formularul impreuna cu datele aferente articolului din baza de date
        // Doar utilizatorii cu rolul de Editor si Admin pot edita articole
        // Adminii pot edita orice articol din baza de date
        // Editorii pot edita doar articolele proprii (cele pe care ei le-au postat)
        // [HttpGet] - se executa implicit

        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id)
        {

            Project project = db.Projects.Include("Department")
                                         .Where(pr => pr.Id == id)
                                         .First();

            project.Categ = GetAllDepartments();

            if ((project.UserId == _userManager.GetUserId(User)) || 
                User.IsInRole("Admin"))
            {
                return View(project);
            }
            else
            {    
                
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }  
        }

        // Se adauga articolul modificat in baza de date
        // Se verifica rolul utilizatorilor care au dreptul sa editeze (Editor si Admin)
        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public IActionResult Edit(int id, Project requestProject)
        {
            var sanitizer = new HtmlSanitizer();

            Project project = db.Projects.Find(id);

            if(ModelState.IsValid)
            {
                if((project.UserId == _userManager.GetUserId(User)) 
                    || User.IsInRole("Admin"))
                {
                    project.Title = requestProject.Title;

                    requestProject.Content = sanitizer.Sanitize(requestProject.Content);

                    project.Content = requestProject.Content;

                    project.Date = DateTime.Now;
                    project.DepartmentId = requestProject.DepartmentId;
                    TempData["message"] = "Articolul a fost modificat";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                else
                {                    
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                requestProject.Categ = GetAllDepartments();
                return View(requestProject);
            }
        }


        // Se sterge un articol din baza de date 
        // Utilizatorii cu rolul de Editor sau Admin pot sterge articole
        // Editorii pot sterge doar articolele publicate de ei
        // Adminii pot sterge orice articol de baza de date

        [HttpPost]
        [Authorize(Roles = "Editor,Admin")]
        public ActionResult Delete(int id)
        {
            // Article article = db.Articles.Find(id);

            Project project = db.Projects.Include("Alerts")
                                         .Where(pr => pr.Id == id)
                                         .First();

            if ((project.UserId == _userManager.GetUserId(User))
                    || User.IsInRole("Admin"))
            {
                db.Projects.Remove(project);
                db.SaveChanges();
                TempData["message"] = "Articolul a fost sters";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un articol care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }    
        }

        // Conditiile de afisare pentru butoanele de editare si stergere
        // butoanele aflate in view-uri
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Editor"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }

        [NonAction]
        public IEnumerable<SelectListItem> GetAllDepartments()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var departments = from cat in db.Departments
                              select cat;

            // iteram prin categorii
            foreach (var department in departments)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = department.Id.ToString(),
                    Text = department.DepartmentName
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var category in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = category.Id.ToString();
                listItem.Text = category.CategoryName;

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        // Metoda utilizata pentru exemplificarea Layout-ului
        // Am adaugat un nou Layout in Views -> Shared -> numit _LayoutNou.cshtml
        // Aceasta metoda are un View asociat care utilizeaza noul layout creat
        // in locul celui default generat de framework numit _Layout.cshtml
        public IActionResult IndexNou()
        {
            return View();
        }
    }
}
