using Antlr.Runtime.Misc;
using MDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDB.Controllers
{

    public class DistributorsController : Controller
    {
        private readonly AppDBEntities DB = new AppDBEntities();

        [OnlineUsers.UserAccess]
        public ActionResult Index()
        {
            return View();
        }

        [OnlineUsers.UserAccess(false/* do not reset timeout*/)]
        public PartialViewResult Distributors(bool forceRefresh = false)
        {
            if (forceRefresh || AppDBDAL.DistributorsHasChanged)
            {
                return PartialView(DB.Distributors.ToList().OrderBy(c => c.Name));
            }
            return null;
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Create()
        {
            ViewBag.Distributions = null;
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(new Distributor());
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Create(Distributor distributor, List<int> SelectedMoviesId)
        {
            if (ModelState.IsValid)
            {
                if (DB.AddDistributor(distributor, SelectedMoviesId) != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de création distributeur" });
            }
            ViewBag.Distributions = null;
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(distributor);
        }

        [OnlineUsers.UserAccess]
        public ActionResult Details(int id)
        {
            Distributor distributor = DB.Distributors.Find(id);
            if (distributor != null)
            {
                return View(distributor);
            }
            return RedirectToAction("Index");
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Edit(int id)
        {
            Distributor distributor = DB.Distributors.Find(id);
            if (distributor != null)
            {
                ViewBag.Distributions = SelectListUtilities<Movie>.Convert(distributor.Movies, "Title");
                ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
                return View(distributor);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Edit(Distributor distributor, List<int> SelectedMoviesId)
        {
            if (ModelState.IsValid)
            {
                if (DB.UpdateDistributor(distributor, SelectedMoviesId))
                    return RedirectToAction("Details/" + distributor.Id);
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de modification distributeur" });
            }
            ViewBag.Distributions = SelectListUtilities<Movie>.Convert(distributor.Movies, "Title");
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(distributor);
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Delete(int id)
        {
            if (DB.RemoveDistributor(id))
                return RedirectToAction("Index");
            else
                return RedirectToAction("Report", "Errors", new { message = "Échec de création distributeur" });
        }
    }
}