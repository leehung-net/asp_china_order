using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using OrderChina.Models;
using PagedList;

namespace OrderChina.Controllers
{
    public class WalletController : Controller
    {
        DBContext db = new DBContext();
        //
        // GET: /Wallet/

        public ActionResult Index(string id)
        {
            if (Session["UserType"] != null && (string)Session["UserType"] == UserType.Client.ToString())
            {
                var listModel = db.Wallets.Where(a => a.Client == User.Identity.Name);
                return View(listModel);
            }
            else if (Session["UserType"] != null && ((string)Session["UserType"] == UserType.Admin.ToString() || (string)Session["UserType"] == UserType.SuperUser.ToString()))
            {

                ViewBag.isadmin = true;

                ViewBag.phone = id;

                var listModel = db.Wallets.Where(a => a.Client == id);
                return View(listModel);
            } if (Session["UserType"] != null && (string)Session["UserType"] == UserType.Accounting.ToString())
            {

                ViewBag.isacc= true;

                ViewBag.phone = id;

                var listModel = db.Wallets.Where(a => a.Client == id);
                return View(listModel);
            }
            return RedirectToAction("Error");
        }

        public ActionResult CreateWallet(string id)
        {
            var model = new Wallet
            {
                Client = id
            };
            ViewBag.ListCurrency = GetListCurrency(id);
            return PartialView("_CreateWallet", model);
        }

        private SelectList GetListCurrency(string id)
        {
            var listWallet = db.Wallets.Where(a => a.Client == id).Select(a => a.Currency).ToString();
            var list =
                db.Currencys.Where(a => !listWallet.Contains(a.Code))
                    .Select(a => new { value = a.Code, display = a.Code + " - " + a.Description });

            return new SelectList(list, "value", "display");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult CreateWallet(Wallet model)
        {
            model.User_Update = User.Identity.Name;
            model.LastUpdate = DateTime.Now;
            db.Wallets.Add(model);
            db.SaveChanges();

            return RedirectToAction("Index", new { id = model.Client });
        }

        public ActionResult AdditionWallet(int id, string cmd)
        {
            ViewBag.isaddition = (cmd == "add" ? true : false);
            var model = new AdditionModel { walletid = id };
            return PartialView("_AdditionWallet", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AdditionWallet(AdditionModel model, string cmd)
        {
            if (cmd == "addition")
            {
                var wallet = db.Wallets.FirstOrDefault(a => a.Id == model.walletid);
                if (wallet != null)
                {
                    wallet.Money = wallet.Money + model.money;
                    wallet.User_Update = User.Identity.Name;
                    wallet.LastUpdate = DateTime.Now;
                    var his = wallet.CloneHistory(model.reason);
                    db.WalletHistorys.Add(his);
                    db.SaveChanges();

                    return RedirectToAction("Index", new { id = wallet.Client });
                }
            }
            else
            {
                var wallet = db.Wallets.FirstOrDefault(a => a.Id == model.walletid);
                if (wallet != null)
                {
                    wallet.Money = wallet.Money - model.money;
                    wallet.User_Update = User.Identity.Name;
                    wallet.LastUpdate = DateTime.Now;
                    var his = wallet.CloneHistory(model.reason);
                    db.WalletHistorys.Add(his);
                    db.SaveChanges();

                    return RedirectToAction("Index", new { id = wallet.Client });
                }
            }

            return RedirectToAction("Error");
        }

        public ActionResult ListClient(string userName, int? page)
        {
            var userProfiles = db.UserProfiles.Where(a => a.UserType == UserType.Client.ToString()).ToList();
            if (!string.IsNullOrEmpty(userName))
            {
                userProfiles = userProfiles.FindAll(a => !String.Equals(a.Phone, User.Identity.Name, StringComparison.CurrentCultureIgnoreCase) && a.Phone.ToLower().Contains(userName.ToLower()));
            }
            
            ViewBag.CurrentUserName = userName;

            const int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(userProfiles.ToPagedList(pageNumber, pageSize));
        }

    }
}
