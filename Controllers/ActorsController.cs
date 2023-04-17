using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MDB.Models;

namespace MoviesDBManager.Controllers
{
    public class ActorsController : Controller
    {
        private readonly AppDBEntities DB = new AppDBEntities();

        [OnlineUsers.UserAccess]
        public ActionResult Index()
        {
            return View();
        }

        [OnlineUsers.UserAccess(false/* do not reset timeout*/)]
        public PartialViewResult Actors(bool forceRefresh = false)
        {
            if (forceRefresh || AppDBDAL.MoviesHasChanged)
            {
                return PartialView(DB.Actors.ToList().OrderBy(c => c.Name));
            }
            return null;
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Create()
        {
            ViewBag.Castings = null;
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(new Actor());
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Create(Actor actor, List<int> SelectedMoviesId)
        {
            if (ModelState.IsValid)
            {
                if (DB.AddActor(actor, SelectedMoviesId) != null)
                    return RedirectToAction("Index");
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de création d'acteur" });
            }
            ViewBag.Castings = null;
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(actor);
        }

        [OnlineUsers.UserAccess]
        public ActionResult Details(int id)
        {
            Actor actor = DB.Actors.Find(id);
            if (actor != null)
            {
                return View(actor);
            }
            return RedirectToAction("Index");
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Edit(int id)
        {
            Actor actor = DB.Actors.Find(id);
            if (actor != null)
            {
                ViewBag.Castings = SelectListUtilities<Movie>.Convert(actor.Movies, "Title");
                ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
                return View(actor);
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Edit(Actor actor, List<int> SelectedMoviesId)
        {
            if (ModelState.IsValid)
            {
                if (DB.UpdateActor(actor, SelectedMoviesId))
                    return RedirectToAction("Details/" + actor.Id );
                else
                    return RedirectToAction("Report", "Errors", new { message = "Échec de modification d'acteur" });
            }
            ViewBag.Castings = SelectListUtilities<Movie>.Convert(actor.Movies, "Title");
            ViewBag.Movies = SelectListUtilities<Movie>.Convert(DB.Movies.ToList(), "Title");
            return View(actor);
        }

        [OnlineUsers.PowerUserAccess]
        public ActionResult Delete(int id)
        {
            if (DB.RemoveActor(id))
                return RedirectToAction("Index");
            else
                return RedirectToAction("Report", "Errors", new { message = "Échec de retrait d'acteur" });
        }
    }
}