using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using OrderChina.Models;
using WebMatrix.WebData;
using System.Text.RegularExpressions;
using System;

namespace OrderChina.Controllers
{
    public class HomeController : Controller
    {
        DBContext db = new DBContext();
        public ActionResult Index()
        {
            if (db.Rates.Any())
            {
                var rate = db.Rates.FirstOrDefault();
                if (rate != null)
                {
                    Session["Price"] = rate.Price.ToString("##,###");
                    Session["fee1"] = rate.FormatPrice(rate.fee1);
                    Session["fee2"] = rate.FormatPrice(rate.fee2);
                    Session["fee3"] = rate.FormatPrice(rate.fee3);
                }
            }
            if (Request.IsAuthenticated)
            {
                var userProfile = db.UserProfiles.FirstOrDefault(a => a.Phone == User.Identity.Name);
                if (userProfile != null)
                {
                    Session["Name"] = userProfile.Name;
                    Session["ID"] = userProfile.UserId;
                    Session["Email"] = userProfile.Email;
                    Session["Phone"] = userProfile.Phone;
                    Session["UserType"] = userProfile.UserType;
                }
            }

            return View();
        }

        public ActionResult About()
        {

            return View();
        }

        public ActionResult Contact()
        {

            return View();
        }
        public ActionResult Introduce()
        {

            return View();
        }
        public ActionResult Banggia()
        {

            return View();
        }
        public ActionResult thanhtoan()
        {

            return View();
        }
        public ActionResult lienhe()
        {

            return View();
        }
        public ActionResult tuyendung()
        {

            return View();
        }
        #region LoginCustomer
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (IsEmail(model.Email))
            {
                if (IsValid(model.Email, model.Password))
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                    if (user != null && user.UserType != "5")
                    {
                        ModelState.AddModelError("", "Bạn không phải khách hàng ");
                    }
                    else  if (user != null)
                        {
                            FormsAuthentication.SetAuthCookie(user.Phone, model.RememberMe);
                            //var authTicket = new FormsAuthenticationTicket(model.Email, model.RememberMe, 1);
                            //var EncryptedTicket = FormsAuthentication.Encrypt(authTicket);
                            //var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, EncryptedTicket);

                            //Response.Cookies.Add(authCookie);
                            if (!string.IsNullOrEmpty(returnUrl))
                            {

                                Session["Name"] = user.Name;
                                Session["ID"] = user.UserId;
                                Session["UserType"] = user.UserType;

                            }
                            return RedirectToAction("Index");
                        }
                    
                    
                
                }
            }
            else
            {
                //phone
                if (IsValidPhone(model.Email, model.Password))
                {

                    FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                    //var authTicket = new FormsAuthenticationTicket(model.Email, model.RememberMe, 1);
                    //var EncryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    //var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, EncryptedTicket);

                    //Response.Cookies.Add(authCookie);
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        var userProfile = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                        if (userProfile != null)
                        {
                            Session["Name"] = userProfile.Name;
                            Session["ID"] = userProfile.UserId;
                            Session["UserType"] = userProfile.UserType;
                        }
                    }
                    return RedirectToAction("Index");
                }
            }


            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Tên người dùng hoặc mật khẩu được cung cấp là không chính xác .");
            return View(model);
        }
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            Session.Remove("Name");
            Session.Remove("ID");
            Session.Remove("UserType");

            return RedirectToAction("Index", "Home");
        }
        private bool IsValidPhone(string phone, string password)
        {
            bool IsValid = false;

            var user = db.UserProfiles.FirstOrDefault(u => u.Phone == phone);
            if (user != null && user.UserType != "5")
            {
                ModelState.AddModelError("", "Bạn không phải khách hàng ");
            }
            else  if (user != null)
            {
                if (user.Password == password)
                {
                    IsValid = true;
                }
            }

            return IsValid;
        }
        private bool IsValid(string email, string password)
        {
            bool IsValid = false;

            var user = db.UserProfiles.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                if (user.Password == password)
                {
                    IsValid = true;
                }
            }

            return IsValid;
        }
        [AllowAnonymous]
        public ActionResult IsCheckExitsPhone(string Phone)
        {
            return Json(!db.UserProfiles.Any(lo => lo.Phone == Phone), JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult IsCheckEmail(string Email)
        {
            if (string.IsNullOrEmpty(Email))
                return Json(true, JsonRequestBehavior.AllowGet);

            var str = Email.ToLower();
            //kiem tra cac ki tu
            const String pattern =
                   @"^([0-9a-zA-Z]" + //Start with a digit or alphabetical
                   @"([\+\-_\.][0-9a-zA-Z]+)*" + // No continuous or ending +-_. chars in email
                   @")+" +
                   @"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";
            if (!Regex.IsMatch(str, pattern))
            {
                return Json("Email không đúng định dạng", JsonRequestBehavior.AllowGet);

            }
            //check trung
            if (db.UserProfiles.Any(lo => lo.Email.ToLower() == Email.ToLower()))
            {
                return Json("Email đã tồn tại trong hệ thống", JsonRequestBehavior.AllowGet);
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }
        public bool IsEmail(string Email)
        {
            var str = Email.ToLower();
            //kiem tra cac ki tu
            const String pattern =
                   @"^([0-9a-zA-Z]" + //Start with a digit or alphabetical
                   @"([\+\-_\.][0-9a-zA-Z]+)*" + // No continuous or ending +-_. chars in email
                   @")+" +
                   @"@(([0-9a-zA-Z][-\w]*[0-9a-zA-Z]*\.)+[a-zA-Z0-9]{2,17})$";

            return Regex.IsMatch(str, pattern);
        }

        #endregion
    }
}
