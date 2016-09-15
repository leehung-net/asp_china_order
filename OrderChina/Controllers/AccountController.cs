using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Data.Linq.SqlClient;
using System.Data.OleDb;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.Web.WebPages.OAuth;
using OrderChina.Common;
using PagedList;
using WebMatrix.WebData;
using OrderChina.Filters;
using OrderChina.Models;
using IsolationLevel = System.Transactions.IsolationLevel;

namespace OrderChina.Controllers
{
    [Authorize]
    [InitializeSimpleMembership]
    public class AccountController : Controller
    {
        readonly DBContext db = new DBContext();
        readonly int pageSize = Convert.ToInt32(WebConfigurationManager.AppSettings["page_size"]);

        #region User
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            if (IsEmail(model.Email))
            {
                if (IsValid(model.Email, model.Password))
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                    if (user != null)
                    {
                        FormsAuthentication.SetAuthCookie(user.Phone, model.RememberMe);
                        //var authTicket = new FormsAuthenticationTicket(model.Email, model.RememberMe, 1);
                        //var EncryptedTicket = FormsAuthentication.Encrypt(authTicket);
                        //var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, EncryptedTicket);

                        //Response.Cookies.Add(authCookie);
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

                        Session["Name"] = user.Name;
                        Session["ID"] = user.UserId;
                        Session["UserType"] = user.UserType;

                    }
                    return RedirectToLocal(returnUrl);
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
                    var userProfile = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                    if (userProfile != null)
                    {
                        Session["Name"] = userProfile.Name;
                        Session["ID"] = userProfile.UserId;
                        Session["UserType"] = userProfile.UserType;
                    }
                    return RedirectToLocal(returnUrl);
                }
            }


            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "The user name or password provided is incorrect.");
            return View(model);
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
        private bool IsValidPhone(string phone, string password)
        {
            bool IsValid = false;

            var user = db.UserProfiles.FirstOrDefault(u => u.Phone == phone);
            if (user != null)
            {
                if (user.Password == password)
                {
                    IsValid = true;
                }
            }

            return IsValid;
        }

        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            Session.Remove("Name");
            Session.Remove("ID");
            Session.Remove("UserType");

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult DeleteUser(string id)
        {
            var model = db.UserProfiles.FirstOrDefault(a => a.Phone == id);
            if (model != null)
            {
                db.UserProfiles.Remove(model);
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register(string phone, string id, string cmd)
        {
            ViewBag.id = id;
            var model = new RegisterModel { Phone = phone, Birthday = DateTime.Now };
            ViewBag.cmd = cmd;
            if (cmd == "client")
            {
                var list = new List<object> { new { name = (int)UserType.Client, display = "Khách hàng" } };
                ViewBag.listUserType = new SelectList(list, "name", "display", (int)UserType.Client);
            }
            else
            {
                ViewBag.listUserType = GetListUserType();
            }
            return View(model);
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model, string id, string cmd)
        {
            if (ModelState.IsValid)
            {
                // Attempt to register the user
                try
                {
                    //insert userProfile
                    var userProfile = new UserProfile
                    {
                        Email = model.Email,
                        Address = model.Address,
                        Phone = model.Phone,
                        Gender = model.Gender,
                        Birthday = model.Birthday,
                        Name = model.Name,
                        Password = model.Password,
                        Account = model.Account
                    };
                    if (model.UserType == null || !model.UserType.Any())
                    {
                        userProfile.UserType = ((int)UserType.Client).ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        string userType = model.UserType.Aggregate(string.Empty, (current, s) => current + "," + s);
                        userType = userType.Substring(1, userType.Length - 1);
                        userProfile.UserType = userType;
                    }
                    db.UserProfiles.Add(userProfile);
                    db.SaveChanges();

                    //create wallet, default currency = VND
                    db.Wallets.Add(new Wallet
                    {
                        LastUpdate = DateTime.Now,
                        User_Update = "",
                        Client = userProfile.Phone,
                        Currency = "VND",
                        Money = 0
                    });

                    if (!Request.IsAuthenticated)
                    {
                        FormsAuthentication.SetAuthCookie(model.Email, false);
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

                        Session["Name"] = userProfile.Name;
                        Session["ID"] = userProfile.UserId;
                        Session["UserType"] = userProfile.UserType;

                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        if (Session["UserType"] != null && Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale, false))
                        {
                            //sale manage
                            var salemanage = new SaleManageClient
                            {
                                User_Client = userProfile.Phone,
                                User_Sale = User.Identity.Name,
                                LastUpdate = DateTime.Now,
                                User_Update = User.Identity.Name
                            };
                            db.SaleManageClients.Add(salemanage);
                            db.SaveChanges();

                            //get order
                            var listOrder =
                                db.Orders.Where(a => a.Phone == userProfile.Phone && string.IsNullOrEmpty(a.SaleManager));
                            foreach (var order in listOrder)
                            {
                                order.SaleManager = User.Identity.Name;
                            }
                            db.SaveChanges();

                            return RedirectToAction("ViewOrderDetail", "Account", new { id });

                        }
                        else
                        {
                            if (cmd == "user")
                            {
                                return RedirectToAction("ListUser", "Account");

                            }
                            return RedirectToAction("ListClient", "Account");
                        }
                    }

                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            // Only disassociate the account if the currently logged in user is the owner
            if (ownerAccount == User.Identity.Name)
            {
                // Use a transaction to prevent the user from deleting their last login credential
                using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
                {
                    bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
                    if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
                    {
                        OAuthWebSecurity.DeleteAccount(provider, providerUserId);
                        scope.Complete();
                        message = ManageMessageId.RemoveLoginSuccess;
                    }
                }
            }

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage(string username, string listStatus, string fromDate, string toDate, string OrderId, int? page)
        {

            List<Order> listOrder = new List<Order>();
            var isFilter = false;
            if (!string.IsNullOrEmpty(username))
            {
                isFilter = true;
                listOrder = db.Orders.Where(a => a.Phone == username).ToList();
            }

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                if (isFilter)
                {
                    listOrder = (from order in listOrder
                                 where fromdate <= order.CreateDate && order.CreateDate <= todate
                                 select order).ToList();
                }
                else
                {
                    isFilter = true;
                    listOrder = (from order in db.Orders
                                 where fromdate <= order.CreateDate && order.CreateDate <= todate
                                 select order).ToList();
                }

            }

            if (!string.IsNullOrEmpty(listStatus))
            {
                if (isFilter)
                {
                    listOrder = listOrder.FindAll(a => a.Status == listStatus);
                }
                else
                {
                    isFilter = true;
                    listOrder = db.Orders.Where(a => a.Status == listStatus).ToList();

                }
            }
            if (!string.IsNullOrEmpty(OrderId))
            {
                if (isFilter)
                {
                    listOrder = listOrder.FindAll(a => a.OrderId.ToString() == OrderId);
                }
                else
                {
                    isFilter = true;
                    listOrder = db.Orders.Where(a => a.OrderId.ToString() == OrderId).ToList();
                }
            }

            ViewBag.ListStatus = GetListStatus(listStatus);

            List<Order> listOrderDisplay = new List<Order>();

            if (isFilter)
            {
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale, false))
                {
                    var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                    listOrderDisplay.AddRange(listOrder.Where(a => listUserManage.Contains(a.Phone) || string.IsNullOrEmpty(a.SaleManager)).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Client, false))
                {
                    listOrderDisplay.AddRange(listOrder.Where(a => a.Phone == User.Identity.Name).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Accounting, false))
                {
                    listOrderDisplay.AddRange(
                        listOrder.Where(
                            a =>
                                a.Status == OrderStatus.SaleConfirm.ToString() || a.Status == OrderStatus.Receive.ToString())
                            .ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Orderer, false))
                {
                    listOrderDisplay.AddRange(listOrder.Where(a => a.Status == OrderStatus.Paid.ToString()).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Recieve, false))
                {
                    listOrderDisplay.AddRange(listOrder.Where(a => a.Status == OrderStatus.Order.ToString() || a.Status == OrderStatus.FullCollect.ToString()).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"]))
                {
                    listOrderDisplay = listOrder.ToList();
                }
            }
            else
            {
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale, false))
                {
                    var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                    listOrderDisplay.AddRange(db.Orders.Where(a => listUserManage.Contains(a.Phone) || string.IsNullOrEmpty(a.SaleManager)).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Client, false))
                {
                    listOrderDisplay.AddRange(db.Orders.Where(a => a.Phone == User.Identity.Name).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Accounting, false))
                {
                    listOrderDisplay.AddRange(
                        db.Orders.Where(
                            a =>
                                a.Status == OrderStatus.SaleConfirm.ToString() || a.Status == OrderStatus.Receive.ToString())
                            .ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Orderer, false))
                {
                    listOrderDisplay.AddRange(db.Orders.Where(a => a.Status == OrderStatus.Paid.ToString()).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Recieve, false))
                {
                    listOrderDisplay.AddRange(db.Orders.Where(a => a.Status == OrderStatus.Order.ToString() || a.Status == OrderStatus.FullCollect.ToString()).ToList());
                }
                if (Utilities.CheckRole((string)Session["UserType"]))
                {
                    listOrderDisplay = db.Orders.ToList();
                }
            }

            listOrderDisplay = listOrderDisplay.Distinct().OrderByDescending(a => a.UpdateDate).ToList();

            ViewBag.CurrentToDate = toDate;
            ViewBag.CurrentFromDate = fromDate;
            ViewBag.CurrentStatus = listStatus;
            ViewBag.CurrentOrderId = OrderId;
            ViewBag.CurrentUserName = username;


            int pageNumber = (page ?? 1);

            return View(listOrderDisplay.ToPagedList(pageNumber, pageSize));
        }
        public ActionResult ManageReceiver(string username, string fromDate, string toDate, string OrderId, int? page)
        {

            List<Order> listOrder = new List<Order>();

            listOrder = db.Orders.Where(a => a.Status == OrderStatus.Receive.ToString() || a.Status == OrderStatus.FullCollect.ToString()).ToList();
            if (!string.IsNullOrEmpty(username))
            {
                listOrder = listOrder.Where(a => a.Phone == username).ToList();
            }
            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                listOrder = (from order in listOrder
                             where fromdate <= order.CreateDate && order.CreateDate <= todate
                             select order).ToList();

            }
            if (!string.IsNullOrEmpty(OrderId))
            {
                listOrder = listOrder.FindAll(a => a.OrderId.ToString() == OrderId);
            }

            if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale, false))
            {
                var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                listOrder = listOrder.FindAll(a => listUserManage.Contains(a.Phone) || string.IsNullOrEmpty(a.SaleManager));
            }
            listOrder = listOrder.OrderByDescending(a => a.UpdateDate).ToList();

            ViewBag.CurrentToDate = toDate;
            ViewBag.CurrentFromDate = fromDate;
            ViewBag.CurrentOrderId = OrderId;
            ViewBag.CurrentUserName = username;


            int pageNumber = (page ?? 1);

            return View(listOrder.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult ManageOrdererReject(string username, string fromDate, string toDate, string OrderId, int? page)
        {
            List<Order> listOrder = new List<Order>();

            listOrder = db.Orders.Where(a => a.Status == OrderStatus.OrdererReject.ToString()).ToList();
            if (!string.IsNullOrEmpty(username))
            {
                listOrder = listOrder.Where(a => a.Phone == username).ToList();
            }
            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);

                listOrder = (from order in listOrder
                             where fromdate <= order.CreateDate && order.CreateDate <= todate
                             select order).ToList();

            }
            if (!string.IsNullOrEmpty(OrderId))
            {
                listOrder = listOrder.FindAll(a => a.OrderId.ToString() == OrderId);
            }

            if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale, false))
            {
                var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                listOrder = listOrder.FindAll(a => listUserManage.Contains(a.Phone) || string.IsNullOrEmpty(a.SaleManager));
            }

            listOrder = listOrder.OrderByDescending(a => a.CreateDate).ToList();

            ViewBag.CurrentToDate = toDate;
            ViewBag.CurrentFromDate = fromDate;
            ViewBag.CurrentOrderId = OrderId;
            ViewBag.CurrentUserName = username;


            int pageNumber = (page ?? 1);

            return View(listOrder.ToPagedList(pageNumber, pageSize));
        }

        private SelectList GetListStatus(string value)
        {
            var list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(OrderStatus).GetFields())
            {
                if (fieldInfo.FieldType.Name != "OrderStatus")
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                list.Add(attribute != null
                    ? new { name = fieldInfo.Name, display = attribute.Name }
                    : new { name = fieldInfo.Name, display = fieldInfo.Name });
            }

            return new SelectList(list, "name", "display", value);
        }

        private SelectList GetListUserType(string[] value = null)
        {
            var list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(UserType).GetFields())
            {
                if (fieldInfo.FieldType.Name != "UserType" || fieldInfo.Name == UserType.Admin.ToString())
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                list.Add(attribute != null
                    ? new { name = (int)fieldInfo.GetValue(fieldInfo), display = attribute.Name }
                    : new { name = (int)fieldInfo.GetValue(fieldInfo), display = fieldInfo.Name });
            }

            return new SelectList(list, "name", "display", value);
        }

        private SelectList GetListUserType(string value = "")
        {
            List<Object> list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(UserType).GetFields())
            {
                if (fieldInfo.FieldType.Name != "UserType" || fieldInfo.Name == UserType.Admin.ToString())
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                list.Add(attribute != null
                    ? new { name = (int)fieldInfo.GetValue(fieldInfo), display = attribute.Name }
                    : new { name = (int)fieldInfo.GetValue(fieldInfo), display = fieldInfo.Name });
            }

            return new SelectList(list, "name", "display", value);
        }
        private SelectList GetListUserType()
        {
            List<Object> list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(UserType).GetFields())
            {
                if (fieldInfo.FieldType.Name != "UserType" || fieldInfo.Name == UserType.Admin.ToString())
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                if (attribute != null)
                    list.Add(new { name = (int)fieldInfo.GetValue(fieldInfo), display = attribute.Name });
                else
                    list.Add(new { name = (int)fieldInfo.GetValue(fieldInfo), display = fieldInfo.Name });

            }

            return new SelectList(list, "name", "display");
        }

        #region Helpers
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }

        internal class ExternalLoginResult : ActionResult
        {
            public ExternalLoginResult(string provider, string returnUrl)
            {
                Provider = provider;
                ReturnUrl = returnUrl;
            }

            public string Provider { get; private set; }
            public string ReturnUrl { get; private set; }

            public override void ExecuteResult(ControllerContext context)
            {
                OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
            }
        }

        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            // See http://go.microsoft.com/fwlink/?LinkID=177550 for
            // a full list of status codes.
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return "User name already exists. Please enter a different user name.";

                case MembershipCreateStatus.DuplicateEmail:
                    return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

                case MembershipCreateStatus.InvalidPassword:
                    return "The password provided is invalid. Please enter a valid password value.";

                case MembershipCreateStatus.InvalidEmail:
                    return "The e-mail address provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidAnswer:
                    return "The password retrieval answer provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidQuestion:
                    return "The password retrieval question provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.InvalidUserName:
                    return "The user name provided is invalid. Please check the value and try again.";

                case MembershipCreateStatus.ProviderError:
                    return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                case MembershipCreateStatus.UserRejected:
                    return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

                default:
                    return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
            }
        }
        #endregion

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

        #region Order
        //
        // GET: /Account/CreateOrder

        [AllowAnonymous]
        public ActionResult CreateOrderManage()
        {
            return View();
        }

        //
        // GET: /Account/CreateOrderFromExcel

        [AllowAnonymous]
        public ActionResult CreateOrderFromExcel()
        {
            return View();
        }


        [HttpPost]
        [AllowAnonymous]
        public JsonResult CreateOrderExcel()
        {
            DataSet ds = new DataSet();
            var httpPostedFileBase = Request.Files["file"];
            if (httpPostedFileBase != null && httpPostedFileBase.ContentLength > 0)
            {
                try
                {
                    string fileExtension =
                                                 System.IO.Path.GetExtension(httpPostedFileBase.FileName);

                    if (fileExtension == ".xls" || fileExtension == ".xlsx")
                    {
                        string fileLocation = Server.MapPath("~/Upload/Images/") + httpPostedFileBase.FileName;
                        if (System.IO.File.Exists(fileLocation))
                        {

                            System.IO.File.Delete(fileLocation);
                        }
                        httpPostedFileBase.SaveAs(fileLocation);

                        string excelConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileLocation + ";Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=2\"";
                        //connection String for xls file format.
                        if (fileExtension == ".xls")
                        {
                            excelConnectionString = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + fileLocation + ";Extended Properties=\"Excel 8.0;HDR=Yes;IMEX=2\"";
                        }
                        //connection String for xlsx file format.
                        else if (fileExtension == ".xlsx")
                        {

                            excelConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + fileLocation + ";Extended Properties=\"Excel 12.0;HDR=Yes;IMEX=2\"";
                        }

                        //Create Connection to Excel work book and add oledb namespace
                        var excelConnection = new OleDbConnection(excelConnectionString);
                        excelConnection.Open();
                        var dt = new DataTable();

                        dt = excelConnection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                        if (dt == null)
                        {
                            return null;
                        }

                        var excelSheets = new String[dt.Rows.Count];
                        int t = 0;
                        //excel data saves in temp file here.
                        foreach (DataRow row in dt.Rows)
                        {
                            excelSheets[t] = row["TABLE_NAME"].ToString();
                            t++;
                        }
                        var excelConnection1 = new OleDbConnection(excelConnectionString);

                        string query = string.Format("Select * from [{0}]", excelSheets[0]);
                        using (var dataAdapter = new OleDbDataAdapter(query, excelConnection1))
                        {
                            dataAdapter.Fill(ds);
                        }

                    }

                    var order = new Order();
                    var orderDetails = new List<OrderDetail>();

                    for (int i = 0; i < ds.Tables[0].Rows.Count; i++)
                    {
                        //lấy thông tin khách hàng
                        if (i == 1)
                        {
                            order.Phone = ds.Tables[0].Rows[i].ItemArray[2].ToString();
                        }
                        else if (i == 2)
                        {
                            order.UserName = ds.Tables[0].Rows[i].ItemArray[2].ToString();
                        }
                        else if (i >= 4)
                        {
                            var orderdetail = new OrderDetail
                            {
                                Link = ds.Tables[0].Rows[i].ItemArray[1].ToString(),
                                Description = ds.Tables[0].Rows[i].ItemArray[2].ToString(),
                                Quantity = Convert.ToInt32(ds.Tables[0].Rows[i].ItemArray[3].ToString()),
                                Price = Convert.ToDouble(ds.Tables[0].Rows[i].ItemArray[4].ToString())
                            };
                            orderDetails.Add(orderdetail);
                        }
                    }

                    //tiến hành update
                    if (string.IsNullOrEmpty(order.Phone) || string.IsNullOrEmpty(order.UserName))
                    {
                        return Json(new { success = false, message = "File không đúng mẫu" });
                    }

                    if (orderDetails.Count(a => string.IsNullOrEmpty(a.Link)) > 0)
                    {
                        return Json(new { success = false, message = "File không đúng mẫu" });
                    }
                    if (orderDetails.Count(a => a.Quantity == null || a.Quantity == 0) > 0)
                    {
                        return Json(new { success = false, message = "File không đúng mẫu" });
                    }
                    if (orderDetails.Count(a => a.Quantity == null || a.Quantity == 0) > 0)
                    {
                        return Json(new { success = false, message = "File không đúng mẫu" });
                    }

                    //kiểm tra phone va email ton tai
                    using (var dbContextTransaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            var rate = db.Rates.FirstOrDefault();
                            order.Rate = rate != null ? rate.Price : 0;
                            order.CreateDate = DateTime.Now;
                            order.UpdateDate = DateTime.Now;
                            order.TotalPrice = orderDetails.Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0));
                            order.Status = OrderStatus.New.ToString();
                            order.Fee = 5;
                            order.TotalPriceConvert = order.TotalPrice * order.Rate;

                            var saleManage = db.SaleManageClients.FirstOrDefault(a => a.User_Client == order.Phone);
                            if (saleManage != null) order.SaleManager = saleManage.User_Sale;
                            db.Entry(order).State = EntityState.Added;
                            db.Orders.Add(order);
                            db.SaveChanges();

                            int orderId = order.OrderId;

                            foreach (var orderDetail in orderDetails)
                            {
                                orderDetail.OrderId = orderId;
                                orderDetail.Phone = order.Phone;
                                orderDetail.OrderDetailStatus = OrderDetailStatus.Active.ToString();
                                db.Entry(orderDetail).State = EntityState.Added;
                                db.OrderDetails.Add(orderDetail);
                                db.SaveChanges();
                            }

                            dbContextTransaction.Commit();

                            if (Request.IsAuthenticated)
                            {
                                return Json(new { success = true, url = Url.Action("Manage","Account")});
                                
                            }
                            return Json(new { success = true });

                        }
                        catch (Exception)
                        {
                            dbContextTransaction.Rollback();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false });
                }
            }

            return Json(new { success = false });
        }
        //
        // GET: /Account/CreateOrder

        [AllowAnonymous]
        public ActionResult CreateOrder()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CreateOrder(NewOrderModel model)
        {
            //thực hiện tạo order dựa tên orderdetail
            if (Request.IsAuthenticated)
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var rate = db.Rates.FirstOrDefault();
                        var user = db.UserProfiles.First(a => a.Phone == User.Identity.Name);
                        var order = new Order
                        {
                            CreateDate = DateTime.Now,
                            UpdateDate = DateTime.Now,
                            TotalPrice = model.ListOrderDetail.Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0)),
                            Status = OrderStatus.New.ToString(),
                            Rate = rate != null ? rate.Price : 0,
                            Fee = 5,
                            Phone = user.Phone,
                            UserName = user.Email
                        };
                        var saleManage = db.SaleManageClients.FirstOrDefault(a => a.User_Client == user.Phone);
                        order.TotalPriceConvert = order.TotalPrice * order.Rate;
                        if (saleManage != null) order.SaleManager = saleManage.User_Sale;
                        db.Entry(order).State = EntityState.Added;
                        db.Orders.Add(order);
                        db.SaveChanges();

                        int orderId = order.OrderId;

                        foreach (var orderDetail in model.ListOrderDetail)
                        {
                            orderDetail.OrderId = orderId;
                            orderDetail.Phone = user.Phone;
                            orderDetail.OrderDetailStatus = OrderDetailStatus.Active.ToString();
                            db.Entry(orderDetail).State = EntityState.Added;
                            db.OrderDetails.Add(orderDetail);
                            db.SaveChanges();
                        }

                        dbContextTransaction.Commit();

                        ViewData["message"] = "Tạo đơn hàng thành công.";
                        return RedirectToAction("Manage", "Account");
                    }
                    catch (Exception)
                    {
                        dbContextTransaction.Rollback();
                    }
                }
            }
            else
            {
                using (var dbContextTransaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var rate = db.Rates.FirstOrDefault();
                        var order = new Order
                        {
                            CreateDate = DateTime.Now,
                            TotalPrice = model.ListOrderDetail.Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0)),
                            Status = OrderStatus.New.ToString(),
                            Rate = rate != null ? rate.Price : 0,
                            Fee = 5,
                            Phone = model.ListOrderDetail.First().Phone,
                            UpdateDate = DateTime.Now
                        };
                        db.Entry(order).State = EntityState.Added;
                        order.TotalPriceConvert = order.TotalPrice * order.Rate;
                        db.Orders.Add(order);
                        db.SaveChanges();

                        int orderId = order.OrderId;

                        foreach (var orderDetail in model.ListOrderDetail)
                        {
                            orderDetail.OrderId = orderId;
                            orderDetail.Phone = order.Phone;
                            orderDetail.OrderDetailStatus = OrderDetailStatus.Active.ToString();

                            db.Entry(orderDetail).State = EntityState.Added;
                            db.OrderDetails.Add(orderDetail);
                            db.SaveChanges();
                        }

                        dbContextTransaction.Commit();

                        ViewData["message"] = "Tạo đơn hàng thành công, chờ nhân viên kinh doanh liên hệ với bạn.";

                        return View();
                    }
                    catch (Exception)
                    {
                        dbContextTransaction.Rollback();
                    }
                }


            }
            return View();
        }

        //
        // GET: /Account/CreateOrder

        [AllowAnonymous]
        public ActionResult CreateOrderOfSale()
        {
            if (Utilities.CheckRole((string)Session["UserType"], (int)UserType.Sale))
            {
                var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                List<object> listClient = new List<object>();
                foreach (var client in listUserManage)
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Phone == client);
                    if (user != null)
                    {
                        listClient.Add(new { value = user.Phone, display = user.Phone + "-" + user.Name });
                    }
                }
                ViewBag.ListClient = new SelectList(listClient, "value", "display");
                return View();

            }

            return RedirectToAction("Error");
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CreateOrderOfSale(NewOrderModel model)
        {
            //thực hiện tạo order dựa tên orderdetail
            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    var rate = db.Rates.FirstOrDefault();
                    var user = db.UserProfiles.First(a => a.Phone == model.Phone);
                    var order = new Order
                    {
                        CreateDate = DateTime.Now,
                        TotalPrice = model.ListOrderDetail.Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0)),
                        Status = OrderStatus.New.ToString(),
                        Rate = rate != null ? rate.Price : 0,
                        Fee = 5,
                        Phone = user.Phone,
                        UserName = user.Email,
                        UpdateDate = DateTime.Now
                    };

                    order.TotalPriceConvert = order.TotalPrice * order.Rate;
                    order.SaleManager = User.Identity.Name;
                    db.Entry(order).State = EntityState.Added;
                    db.Orders.Add(order);
                    db.SaveChanges();

                    int orderId = order.OrderId;

                    foreach (var orderDetail in model.ListOrderDetail)
                    {
                        orderDetail.OrderId = orderId;
                        orderDetail.Phone = user.Phone;
                        orderDetail.OrderDetailStatus = OrderDetailStatus.Active.ToString();
                        db.Entry(orderDetail).State = EntityState.Added;
                        db.OrderDetails.Add(orderDetail);
                        db.SaveChanges();
                    }

                    dbContextTransaction.Commit();

                    ViewData["message"] = "Tạo đơn hàng thành công.";
                    return RedirectToAction("Manage", "Account");
                }
                catch (Exception)
                {
                    dbContextTransaction.Rollback();
                }
            }

            return View();
        }

        //private Stream GenerateStreamFromString(string s)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    StreamWriter writer = new StreamWriter(stream);
        //    writer.Write(s);
        //    writer.Flush();
        //    stream.Position = 0;
        //    return stream;
        //}


        public ActionResult ViewOrderDetail(int id)
        {
            var model = new ViewDetailOrderModel();
            var order = db.Orders.FirstOrDefault(a => a.OrderId == id);
            var orderDetail = db.OrderDetails.Where(a => a.OrderId == id);
            model.OrderId = id;
            if (order != null)
            {
                model.Phone = order.Phone;
                model.Rate = order.Rate;
                model.Status = order.Status;
                model.StatusText = order.getStatusText();
                model.TotalPriceConvert = order.TotalPriceConvert;
                model.TotalPrice = order.TotalPrice;
                model.SaleManager = order.SaleManager;
                model.UserName = order.UserName;
                model.Phone = order.Phone;
                model.FeeShip = order.FeeShip;
                model.FeeShipChina = order.FeeShipChina;
                model.CreateDate = order.CreateDate;
                model.Fee = order.Fee;
                model.Weight = order.Weight;
                model.DownPayment = order.DownPayment;
                model.AccountingCollected = order.AccountingCollected;


                if (!string.IsNullOrEmpty(order.SaleManager))
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Phone == order.SaleManager);
                    if (user != null)
                    {
                        model.SaleManageInfo = new SaleManageInfo
                        {
                            SaleName = user.Name,
                            SalePhone = user.Phone,
                            SalePhoneCompany = user.PhoneCompany
                        };
                    }
                }

                if (!string.IsNullOrEmpty(order.Phone))
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Phone == order.Phone);
                    if (user != null)
                    {
                        model.Client = user;
                    }
                }
            }
            model.ListOrderDetails = orderDetail;
            return View(model);
        }


        public ActionResult SaleConfirmOrder(int id)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId == id);
            return PartialView("_SaleConfirmOrderPartial", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult SaleConfirmOrder(Order model)
        {
            var order = db.Orders.FirstOrDefault(m => m.OrderId == model.OrderId);
            if (order != null)
            {
                order.Fee = model.Fee;
                order.FeeShip = model.FeeShip;
                order.FeeShipChina = model.FeeShipChina;
                order.UpdateDate = DateTime.Now;
                order.Status = OrderStatus.SaleConfirm.ToString();
                db.SaveChanges();
            }

            return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });

        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult OrdererRejectOrder(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString() == orderid);
            if (model != null)
            {
                model.Status = OrderStatus.OrdererReject.ToString();
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult OrdererConfirmOrder(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString() == orderid);
            if (model != null)
            {
                model.Status = OrderStatus.Order.ToString();
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult ConfirmReceive(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString() == orderid);
            if (model != null)
            {
                model.Status = OrderStatus.Receive.ToString();
                db.SaveChanges();

                //gửi mail
                SendMail(model.UserName, model.OrderId);
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult FinishOrder(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString() == orderid);
            if (model != null)
            {

                model.Status = OrderStatus.Finish.ToString();
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        public ActionResult UpdateOrder(int id)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId == id);
            return PartialView("_UpdateOrderPartial", model);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrder(Order model)
        {
            var order = db.Orders.FirstOrDefault(m => m.OrderId == model.OrderId);
            if (order != null)
            {

                order.FeeShip = model.FeeShip;
                order.Weight = model.Weight;
                order.FeeShipChina = model.FeeShipChina;
                order.UpdateDate = DateTime.Now;

                order.Status = OrderStatus.Order.ToString();
                db.SaveChanges();
            }

            return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });

        }

        public ActionResult AccountingConfirmOrder(int id)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId == id);
            return PartialView("_AccountingConfirmOrderPartial", model);
        }


        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AccountingConfirmOrder(Order model)
        {
            var order = db.Orders.FirstOrDefault(m => m.OrderId == model.OrderId);
            if (order != null)
            {
                if (model.Status == OrderStatus.SaleConfirm.ToString())
                {
                    order.DownPayment = model.DownPayment;
                    var sale = db.UserProfiles.FirstOrDefault(a => a.Phone == order.SaleManager);
                    var client = db.UserProfiles.FirstOrDefault(a => a.Phone == order.Phone);

                    order.TransId = Utilities.GenerateOrderId(order.OrderId, (sale == null ? 0 : sale.UserId),
                        (client == null ? 0 : client.UserId));
                    order.Status = OrderStatus.Paid.ToString();

                }
                else if (model.Status == OrderStatus.Receive.ToString())
                {
                    order.AccountingCollected = model.AccountingCollected;
                    order.Status = OrderStatus.FullCollect.ToString();
                }

                order.UpdateDate = DateTime.Now;

                db.SaveChanges();
            }

            return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });

        }
        public ActionResult AddEditOrderDetail(int? id, int? orderId)
        {
            if (id != null)
            {
                ViewBag.IsUpdate = true;
                var model = db.OrderDetails.FirstOrDefault(m => m.OrderDetailId == id);
                return PartialView("_AddOrderDetailPartial", model);
            }
            ViewBag.IsUpdate = false;
            var modelEmpty = new OrderDetail { OrderId = orderId ?? 0 };
            return PartialView("_AddOrderDetailPartial", modelEmpty);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AddEditOrderDetail(OrderDetail model, string cmd)
        {
            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                if (cmd == "Save")
                {
                    try
                    {
                        //
                        //var user = db.UserProfiles.FirstOrDefault(a => a.Phone == User.Identity.Name);
                        //if (user != null) model.em = user.Phone;
                        model.OrderDetailStatus = OrderDetailStatus.Active.ToString();
                        db.OrderDetails.Add(model);
                        db.SaveChanges();

                        //tính toán lại order
                        var ord = db.Orders.FirstOrDefault(a => a.OrderId == model.OrderId);
                        if (ord != null)
                        {
                            ord.TotalPrice =
                                db.OrderDetails.Where(a => a.OrderId == model.OrderId && a.OrderDetailStatus == OrderDetailStatus.Active.ToString()).Sum(a => a.Quantity * a.Price ?? 0);
                            ord.TotalPriceConvert = ord.Rate * ord.TotalPrice;
                            db.SaveChanges();
                        }
                        dbContextTransaction.Commit();
                        ViewData["message"] = "Thêm mới link hàng thành công";
                        return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });
                    }
                    catch
                    {
                        ViewData["message"] = "Thêm mới link hàng thất bại";
                        dbContextTransaction.Rollback();
                    }
                }
                else
                {
                    try
                    {
                        var orderDetail = db.OrderDetails.FirstOrDefault(m => m.OrderDetailId == model.OrderDetailId);
                        if (orderDetail != null)
                        {
                            orderDetail.Link = model.Link;
                            orderDetail.Shop = model.Shop;
                            orderDetail.Description = model.Description;
                            orderDetail.Quantity = model.Quantity;
                            orderDetail.Price = model.Price;
                            orderDetail.OrderDetailStatus = OrderDetailStatus.Active.ToString();
                            db.SaveChanges();

                            var ord = db.Orders.FirstOrDefault(a => a.OrderId == model.OrderId);
                            if (ord != null)
                            {
                                ord.TotalPrice =
                                    db.OrderDetails.Where(a => a.OrderId == model.OrderId && a.OrderDetailStatus == OrderDetailStatus.Active.ToString())
                                        .Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0));
                                ord.TotalPriceConvert = ord.Rate * ord.TotalPrice;
                                db.SaveChanges();
                            }
                        }

                        dbContextTransaction.Commit();
                        return RedirectToAction("ViewOrderDetail", new { id = model.OrderId, message = "Cập nhật link hàng thành công" });
                    }
                    catch
                    {
                        ViewData["message"] = "Cập nhật link hàng thất bại";
                        dbContextTransaction.Rollback();
                    }
                }

            }

            return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });
        }

        public ActionResult UpdateOrderDetail(int? id, bool IsOrderer)
        {
            var model = db.OrderDetails.FirstOrDefault(m => m.OrderDetailId == id);
            ViewBag.IsOrderer = IsOrderer;
            return PartialView("_UpdateOrderDetail", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderDetail(OrderDetail model)
        {
            using (var dbContextTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    var orderDetail = db.OrderDetails.FirstOrDefault(m => m.OrderDetailId == model.OrderDetailId);
                    if (orderDetail != null)
                    {
                        orderDetail.DeliveryDate = model.DeliveryDate;
                        orderDetail.QuantityInWarehouse = model.QuantityInWarehouse;
                        orderDetail.QuantitySellPlace = model.QuantitySellPlace;
                        orderDetail.Rate_Real = model.Rate_Real;
                        db.SaveChanges();
                    }

                    dbContextTransaction.Commit();
                }
                catch
                {
                    ViewData["message"] = "Cập nhật link hàng thất bại";
                    dbContextTransaction.Rollback();
                }

            }

            return RedirectToAction("ViewOrderDetail", new { id = model.OrderId });
        }

        public ActionResult DeletedOrderDetail(int id)
        {
            var orderDetail = db.OrderDetails.FirstOrDefault(m => m.OrderDetailId == id);
            var orderId = 0;
            if (orderDetail != null)
            {
                orderId = orderDetail.OrderId;
                orderDetail.OrderDetailStatus = OrderDetailStatus.Inactive.ToString();
                //db.OrderDetails.Remove(orderDetail);
                db.SaveChanges();

                var ord = db.Orders.FirstOrDefault(a => a.OrderId == orderDetail.OrderId);
                if (ord != null)
                {
                    ord.TotalPrice = db.OrderDetails.Where(a => a.OrderId == orderDetail.OrderId && a.OrderDetailStatus == OrderDetailStatus.Active.ToString()).Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0));
                    ord.TotalPriceConvert = ord.Rate * ord.TotalPrice;
                    db.SaveChanges();
                }
            }
            return RedirectToAction("ViewOrderDetail", new { id = orderId });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Cancel_Order(string orderId)
        {
            var order = db.Orders.FirstOrDefault(m => m.OrderId.ToString() == orderId);
            if (order != null)
            {
                order.Status = OrderStatus.Cancel.ToString();
                db.SaveChanges();
            }
            return Json(new { success = true });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Confirm_Order(string orderId)
        {
            var order = db.Orders.FirstOrDefault(m => m.OrderId.ToString() == orderId);
            if (order != null)
            {
                order.Status = OrderStatus.ClientConfirm.ToString();
                db.SaveChanges();
            }
            return Json(new { success = true });
        }
        #endregion

        #region Administrator

        public ActionResult ListClient(string userName, int? page)
        {
            var userProfiles = new List<UserProfile>();

            userProfiles = (from user in db.UserProfiles
                            where user.UserType.Contains(((int)UserType.Client).ToString())
                            select user).ToList();

            if (!string.IsNullOrEmpty(userName))
            {
                userProfiles = userProfiles.FindAll(a => !String.Equals(a.Phone, User.Identity.Name, StringComparison.CurrentCultureIgnoreCase) && a.Phone.ToLower().Contains(userName.ToLower()));
            }
            foreach (var userProfile in userProfiles)
            {
                //get sale
                var salemanage =
                    db.SaleManageClients.FirstOrDefault(a => a.User_Client == userProfile.Phone);
                if (salemanage != null)
                {
                    userProfile.SaleManage = salemanage.User_Sale;
                }
            }

            ViewBag.CurrentUserName = userName;

            int pageNumber = (page ?? 1);

            return View(userProfiles.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult ListUser(string userType, string userName, int? page)
        {
            ViewBag.ListUserType = GetListUserType(userType);
            var userProfiles = new List<UserProfile>();

            userProfiles = (from user in db.UserProfiles
                            where !user.UserType.Contains(((int)UserType.Client).ToString()) && user.UserType.Contains(userType ?? user.UserType)
                            select user).ToList();

            if (!string.IsNullOrEmpty(userName))
            {
                userProfiles = userProfiles.FindAll(a => !String.Equals(a.Phone, User.Identity.Name, StringComparison.CurrentCultureIgnoreCase) && a.Phone.ToLower().Contains(userName.ToLower()));
            }

            ViewBag.CurrentUserType = userType;
            ViewBag.CurrentUserName = userName;

            int pageNumber = (page ?? 1);

            return View(userProfiles.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult AssignSaleForClient(string id, string userSale)
        {
            var model = new SaleManageClient { User_Client = id, User_Sale = userSale };
            var listSale = db.UserProfiles.Where(a => a.UserType.Contains(((int)UserType.Sale).ToString())).Select(a => new { phone = a.Phone, display = a.Name });
            ViewBag.listSale = new SelectList(listSale, "phone", "display", userSale);
            return PartialView("_AssignSalePartial", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult AssignSaleForClient(SaleManageClient model, string listSale)
        {
            var update = db.SaleManageClients.FirstOrDefault(a => a.User_Client == model.User_Client);

            if (update == null)
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        model.User_Update = User.Identity.Name;
                        model.LastUpdate = DateTime.Now;
                        db.SaleManageClients.Add(model);

                        //truong hop khach hang moi duoc gan sale
                        //update sale vao don hang moi chua co sale manage
                        var listOrder = db.Orders.Where(a => a.Phone == model.User_Client && string.IsNullOrEmpty(a.SaleManager)).ToList();
                        foreach (var order in listOrder)
                        {
                            order.SaleManager = model.User_Sale;
                        }
                        db.SaveChanges();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                    }
                }

            }
            else
            {
                update.User_Update = User.Identity.Name;
                update.LastUpdate = DateTime.Now;
                update.User_Sale = model.User_Sale;
                db.SaveChanges();
            }

            return RedirectToAction("ListClient");
        }

        public ActionResult ChangeUserType(string id, string userType)
        {
            var user = db.UserProfiles.FirstOrDefault(a => a.Phone == id);
            if (user == null)
                return RedirectToAction("ListUser");
            var model = new ChangeUserTypeModel
            {
                Phone = user.Phone
            };
            var select = userType.Split(',');
            ViewBag.listUserType = GetListUserType(select);

            return PartialView("_ChangeUserTypePartial", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeUserType(ChangeUserTypeModel model)
        {
            var userUpdate = db.UserProfiles.FirstOrDefault(a => a.Phone == model.Phone);
            if (userUpdate != null)
            {
                if (model.UserType == null || !model.UserType.Any())
                {
                    userUpdate.UserType = ((int)UserType.Client).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    string userType = model.UserType.Aggregate(string.Empty, (current, s) => current + "," + s);
                    userType = userType.Substring(1, userType.Length - 1);
                    userUpdate.UserType = userType;
                }

                db.SaveChanges();

            }
            return RedirectToAction("ListUser");
        }

        public ActionResult UpdateRate(string fromDate, string toDate, int? page)
        {
            int pageNumber = (page ?? 1);

            var model = new RateModel();
            var rate = db.Rates.First();
            if (rate != null)
            {
                model.Rate = rate;
                if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
                {
                    var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                    var rateHistories = (from rateHistory in db.RateHistorys
                                         where fromdate <= rateHistory.LastUpdate && rateHistory.LastUpdate <= todate
                                         select rateHistory).ToList();
                    model.ListRateHistory = rateHistories.ToPagedList(pageNumber, pageSize);

                }

                model.ListRateHistory = db.RateHistorys.OrderByDescending(a => a.LastUpdate).Take(10).ToPagedList(pageNumber, pageSize);
            }
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateRate(RateModel model)
        {
            if (model.Rate.RateId > 0)
            {
                var modelUpdate = db.Rates.FirstOrDefault(a => a.RateId == model.Rate.RateId);

                if (modelUpdate != null)
                {
                    modelUpdate.Price = model.Rate.Price;
                    modelUpdate.fee1 = model.Rate.fee1;
                    modelUpdate.fee2 = model.Rate.fee2;
                    modelUpdate.fee3 = model.Rate.fee3;
                    modelUpdate.userUpdate = User.Identity.Name;
                    modelUpdate.lastUpdated = DateTime.Now;

                    db.RateHistorys.Add(modelUpdate.CloneHistory());
                    db.SaveChanges();

                    Session["Price"] = modelUpdate.Price.ToString("##,###");
                    Session["fee1"] = modelUpdate.FormatPrice(modelUpdate.fee1);
                    Session["fee2"] = modelUpdate.FormatPrice(modelUpdate.fee2);
                    Session["fee3"] = modelUpdate.FormatPrice(modelUpdate.fee3);
                }
            }
            else
            {
                model.Rate.userUpdate = User.Identity.Name;
                model.Rate.lastUpdated = DateTime.Now;
                db.Rates.Add(model.Rate);

                db.RateHistorys.Add(model.Rate.CloneHistory());
                db.SaveChanges();

                Session["Price"] = model.Rate.Price.ToString("##,###");
                Session["fee1"] = model.Rate.FormatPrice(model.Rate.fee1);
                Session["fee2"] = model.Rate.FormatPrice(model.Rate.fee2);
                Session["fee3"] = model.Rate.FormatPrice(model.Rate.fee3);
            }

            return RedirectToAction("UpdateRate");
        }
        #endregion

        #region Personal
        public ActionResult Personal(int id)
        {
            return View(db.UserProfiles.Find(id));
        }

        [HttpPost]
        public ActionResult Personal(int id, FormCollection collection)
        {

            if (ModelState.IsValid)
            {

                var user = db.UserProfiles.First(m => m.UserId == id);
                string name = collection["Name"];
                string phone = collection["Phone"];
                string mail = collection["Email"];
                string gerna = collection["Gender"];
                string addres = collection["Address"];

                user.Name = name;
                user.Phone = phone;
                user.Email = mail;
                user.Gender = gerna;
                user.Address = addres;
                db.SaveChanges();
            }
            return RedirectToAction("Personal");
        }
        #endregion

        #region ChangePass

        public ActionResult ChangePass()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ChangePass(string id, FormCollection collection)
        {
            if (ModelState.IsValid)
            {
                var user = db.UserProfiles.First(m => m.Phone == id);
                string oldpass = collection["Password"];
                string newpass = collection["NewPassword"];
                if (user.Password != oldpass)
                {
                    ModelState.AddModelError("", "Mật khẩu cũ sai mời bạn nhập lại đầy đủ thông tin");

                }
                else
                {
                    user.Password = newpass;
                    db.SaveChanges();
                    return RedirectToAction("Manage");
                }

            }
            return View();

        }
        #endregion

        #region DepositOrders
        [AllowAnonymous]
        public ActionResult DepositOrders()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult DepositOrders(NewDepositOrders model)
        {
            var email = Session["Email"].ToString();
            if (ModelState.IsValid)
            {
                foreach (var order in model.ListDepositOrders)
                {
                    order.EmailUser = email;
                    order.Status = "Đơn mới";
                    order.TotalPriceConvert = 0;
                    db.DepositOrders.Add(order);
                    order.CreateDate = DateTime.Now;
                    db.SaveChanges();
                    return RedirectToAction("MangeDepositOrders", "Account");
                }
            }
            return View();
        }
        #endregion

        #region ManageDepositOrders
        public ActionResult MangeDepositOrders(int? page)
        {
            var email = Session["Email"].ToString();
            var type = Session["UserType"].ToString();
            if (type == "Client")
            {
                int pageSize = 8;
                int pageNum = (page ?? 1);
                List<DepositOrders> deporder = new List<DepositOrders>();
                deporder = (from a in db.DepositOrders where a.EmailUser == email select a).ToList();
                return View(deporder.ToPagedList(pageNum, pageSize));
            }
            else
            {
                int pageSize = 8;
                int pageNum = (page ?? 1);
                List<DepositOrders> deporder = new List<DepositOrders>();
                deporder = (from a in db.DepositOrders select a).ToList();
                return View(deporder.ToPagedList(pageNum, pageSize));
            }


        }
        #endregion

        #region LoginManager
        [AllowAnonymous]
        public ActionResult LoginManager(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult LoginManager(LoginModel model, string returnUrl)
        {
            if (IsEmail(model.Email))
            {
                if (IsValid(model.Email, model.Password))
                {
                    var user = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                    if (user == null || user.UserType == "1" || user.UserType == "5")
                    {
                        ModelState.AddModelError("", "Bạn không phải quản lý quản lý bạn không thể đăng nhập ở đây");
                    }
                    else if (user != null)
                    {
                        FormsAuthentication.SetAuthCookie(user.Phone, model.RememberMe);
                        //var authTicket = new FormsAuthenticationTicket(model.Email, model.RememberMe, 1);
                        //var EncryptedTicket = FormsAuthentication.Encrypt(authTicket);
                        //var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, EncryptedTicket);

                        //Response.Cookies.Add(authCookie);
                        if (!string.IsNullOrEmpty(returnUrl))
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

                            Session["Name"] = user.Name;
                            Session["ID"] = user.UserId;
                            Session["UserType"] = user.UserType;

                        }
                        return RedirectToAction("Manage", "Account");
                    }

                }

            }
            else
            {
                //phone
                if (IsValidPhoneManager(model.Email, model.Password))
                {

                    FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);
                    //var authTicket = new FormsAuthenticationTicket(model.Email, model.RememberMe, 1);
                    //var EncryptedTicket = FormsAuthentication.Encrypt(authTicket);
                    //var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, EncryptedTicket);

                    //Response.Cookies.Add(authCookie);
                    if (!string.IsNullOrEmpty(returnUrl))
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
                        var userProfile = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
                        if (userProfile != null)
                        {
                            Session["Name"] = userProfile.Name;
                            Session["ID"] = userProfile.UserId;
                            Session["UserType"] = userProfile.UserType;
                        }
                        return RedirectToAction("Manage", "Account");
                    }

                }
            }


            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Tên người dùng hoặc mật khẩu được cung cấp là không chính xác .");
            return View(model);
        }
        private bool IsValidPhoneManager(string phone, string password)
        {
            bool IsValid = false;

            var user = db.UserProfiles.FirstOrDefault(u => u.Phone == phone);
            if (user == null || user.UserType == "1" || user.UserType == "5")
            {
                ModelState.AddModelError("", "Bạn không phải quản lý quản lý bạn không thể đăng nhập ở đây");
            }
            else if (user != null)
            {
                if (user.Password == password)
                {
                    IsValid = true;
                }
            }
            return IsValid;
        }
        #endregion

        #region wallet

        #endregion

        public bool SendMail(string address, int orderid, List<string> listLink = null)
        {
            try
            {
                var userProfile = db.UserProfiles.FirstOrDefault(a => a.Email == address);
                if (userProfile != null)
                {
                    var fromAddress = new MailAddress(WebConfigurationManager.AppSettings["Email"], WebConfigurationManager.AppSettings["Email_Name"]);
                    var toAddress = new MailAddress(userProfile.Email, userProfile.Name);

                    var fromPassword = WebConfigurationManager.AppSettings["Password"];
                    var subject = WebConfigurationManager.AppSettings["Subject"];
                    string body = "Công ty abc xin trân trọng kính báo đơn hàng : " + orderid + " đã về đến kho của chúng tôi. \n Kính mới quý khách đến lấy. Chúc quý khách làm ăn phát đạt. \n Regards.";

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };
                    using (var message = new MailMessage(fromAddress, toAddress) { Subject = subject, Body = body, IsBodyHtml = true })
                    {
                        smtp.Send(message);
                        return true;
                    }
                }

            }
            catch (Exception)
            {

            }
            return false;
        }

    }

}
