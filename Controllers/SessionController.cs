using MDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MDB.Controllers
{
    public class SessionController : Controller
    {
        private readonly AppDBEntities DB = new AppDBEntities();
        public ActionResult End(string message)
        {
            if (Session["currentLoginId"] != null)
                DB.UpdateLogout((int)Session["currentLoginId"]);
            OnlineUsers.RemoveSessionUser();
            return RedirectToAction("Login", "Accounts", new { message });
        }
    }
}