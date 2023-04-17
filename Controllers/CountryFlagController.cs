using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using MDB.Models;

namespace MDB.Controllers
{
    public class CountryFlagController : Controller
    {
        public ActionResult Get(string countryCode)
        {
            return Json(Countries.FlagUrl(countryCode), JsonRequestBehavior.AllowGet);
        }
    }
}