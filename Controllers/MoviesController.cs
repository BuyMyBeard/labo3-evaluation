using MDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MoviesDBManager.Controllers
{
    public class MoviesController : Controller
    {
        private readonly AppDBEntities DB = new AppDBEntities();

        [OnlineUsers.UserAccess]
        public ActionResult Index()
        {
            return View();
        }

        [OnlineUsers.UserAccess(false/* do not reset timeout*/)]
        public PartialViewResult Movies(bool forceRefresh = false)
        {
            if (forceRefresh || AppDBDAL.MoviesHasChanged)
            {
                return PartialView(DB.Movies.ToList().OrderBy(c => c.Title));
            }
            return null;
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Create()
        {
            ViewBag.Castings = null;
            ViewBag.Actors = SelectListUtilities<Actor>.Convert(DB.Actors.ToList());
            ViewBag.Distributions = null;
            ViewBag.Distributors = SelectListUtilities<Distributor>.Convert(DB.Distributors.ToList());
            var movie = new Movie();
            movie.ReleaseYear = DateTime.Now.Year;
            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Create(Movie movie, List<int> SelectedActorsId, List<int> SelectedDistributorsId)
        {
            if (ModelState.IsValid)
            {
                if (DB.AddMovie(movie, SelectedActorsId, SelectedDistributorsId) != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de création de film" });
            }
            ViewBag.Castings = null;
            ViewBag.Actors = SelectListUtilities<Actor>.Convert(DB.Actors.ToList());
            ViewBag.Distributions = null;
            ViewBag.Distributors = SelectListUtilities<Distributor>.Convert(DB.Distributors.ToList());
            return View(movie);
        }

        [OnlineUsers.UserAccess]
        public ActionResult Details(int id)
        {
            Movie movie = DB.Movies.Find(id);
            if (movie != null)
            {
                return View(movie);
            }
            return RedirectToAction("Index");
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Edit(int id)
        {
            Movie movie = DB.Movies.Find(id);
            if (movie != null)
            {
                ViewBag.Castings = SelectListUtilities<Actor>.Convert(movie.Actors);
                ViewBag.Actors = SelectListUtilities<Actor>.Convert(DB.Actors.ToList());
                ViewBag.Distributions = SelectListUtilities<Distributor>.Convert(movie.Distributors);
                ViewBag.Distributors = SelectListUtilities<Distributor>.Convert(DB.Distributors.ToList());
                return View(movie);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Edit(Movie movie, List<int> SelectedActorsId, List<int> SelectedDistributorsId)
        {
            if (ModelState.IsValid)
            {
                if (DB.UpdateMovie(movie, SelectedActorsId, SelectedDistributorsId))
                    return RedirectToAction("Details/" + movie.Id);
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de modification de film" });
            }
            ViewBag.Castings = SelectListUtilities<Actor>.Convert(movie.Actors);
            ViewBag.Actors = SelectListUtilities<Actor>.Convert(DB.Actors.ToList());
            ViewBag.Distributions = SelectListUtilities<Distributor>.Convert(movie.Distributors);
            ViewBag.Distributors = SelectListUtilities<Distributor>.Convert(DB.Distributors.ToList());
            return View(movie);
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Delete(int id)
        {
            if (DB.RemoveMovie(id))
                return RedirectToAction("Index");
            else
                return RedirectToAction("Report", "Errors", new { message = "Échec de retrait de film" });
        }
    }
}