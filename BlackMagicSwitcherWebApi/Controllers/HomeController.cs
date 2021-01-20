using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BlackMagicSwitcherWebApi.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Camera Home";
            return View();
        }


        public ActionResult Cam1()
        {
            ViewBag.Title = "Camera 1";
            return View();
        }


        public ActionResult Cam2()
        {
            ViewBag.Title = "Camera 2";
            return View();
        }

        public ActionResult Cam3()
        {
            ViewBag.Title = "Camera 3";
            return View();
        }
    }
}
