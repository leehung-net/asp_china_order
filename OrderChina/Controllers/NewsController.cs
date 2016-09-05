using OrderChina.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;

namespace OrderChina.Controllers
{
    public class NewsController : Controller
    {
        DBContext db = new DBContext();

        public ActionResult Index(int? page)
        {
            int pageSize = 8;
            int pageNum = (page ?? 1);
            List<News> lisnew = new List<News>();
            lisnew = (from a in db.News select a).ToList();
            return View(lisnew.ToPagedList(pageNum, pageSize));
        }
        public ActionResult DetailNews(int id)
        {

            var news = from a in db.News where a.IDNews == id select a;
            return View(news);
        }
        public ActionResult ManageNews(int? page)
        {
            int pageSize = 20;
            int pageNum = (page ?? 1);
            List<News> lisnew = new List<News>();
            lisnew = (from a in db.News select a).ToList();
            return View(lisnew.ToPagedList(pageNum, pageSize));
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(News model,FormCollection collection)
        {

            if (ModelState.IsValid)
            {
                db.News.Add(model);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        #region Edit
        [HttpGet]
        public ActionResult Edit(int id)
        {
            return View(db.News.Find(id));
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(News model,int id)
        {
            var news = from a in db.News where a.IDNews == id select a; 
            if (ModelState.IsValid)
            {
                UpdateModel(news);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        #endregion
        #region Delete
        public ActionResult Delete(int id)
        {
            var product = db.News.First(p => p.IDNews == id);
            db.News.Remove(product);
            db.SaveChanges();
            return RedirectToAction("Index");

        }
        #endregion
    }
}
