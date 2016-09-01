using OrderChina.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OrderChina.Controllers
{
    public class NewsController : Controller
    {
        DBContext db = new DBContext();

        public ActionResult Index()
        {
            var news = from a in db.News select a;
            return View(news);
        }
    }
}
