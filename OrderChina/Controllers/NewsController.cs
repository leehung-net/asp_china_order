using OrderChina.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PagedList;
using PagedList.Mvc;
using OrderChina.Filters;

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
            //var sp = from a in db.News select a;
            //
            return View(lisnew.ToPagedList(pageNum, pageSize));
        }
        public ActionResult DetailNews(int id)
        {

            var news = from a in db.News where a.IDNews == id select a;
            return View(news);
        }
        [Authorize]  
        [InitializeSimpleMembership]
        public ActionResult ManageNews(int? page)
        {
            int pageSize = 20;
            int pageNum = (page ?? 1);
            List<News> lisnew = new List<News>();
            lisnew = (from a in db.News select a).ToList();
            return View(lisnew.ToPagedList(pageNum, pageSize));
        }
        [HttpGet]
        public ActionResult Create()
        {
            return View();
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(News model, FormCollection collection)
        {        
            if (ModelState.IsValid)
            {
                string title = collection["Title"];
                string titlebig = collection["Titlebig"];
                string content = collection["NewsContent"];
                string img = collection["Img"];
                model.Title = title;
                model.Titlebig = titlebig;
                model.NewsContent = content;
                model.Img = img;
                db.News.Add(model);
                db.SaveChanges();
            }
            return RedirectToAction("ManageNews");
        }
        #region Edit
        [HttpGet]
        public ActionResult Edit(int id)
        {
            return View(db.News.Find(id));
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(int id, FormCollection collection)
        {
            
            if (ModelState.IsValid)
            {
                var news = db.News.First(m => m.IDNews == id);
                string title = collection["Title"];
                string titlebig = collection["Titlebig"];
                string content = collection["NewsContent"];
                string img = collection["Img"];
                news.Title = title;
                news.Titlebig = titlebig;
                news.NewsContent = content;
                news.Img = img;
                UpdateModel(news);
                db.SaveChanges();
            }
            return RedirectToAction("ManageNews");
        }
        #endregion
        #region Delete
        public ActionResult Delete(int id)
        {
            var product = db.News.First(p => p.IDNews == id);
            db.News.Remove(product);
            db.SaveChanges();
            return RedirectToAction("ManageNews");

        }
        #endregion
       
    }
}
