using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.Web.WebPages.OAuth;
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

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register(string phone)
        {

            var model = new RegisterModel { Phone = phone, Birthday = DateTime.Now };
            ViewBag.listUserType = GetListUserType();

            return View(model);
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model, string listUserType)
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
                        Account = model.Account,
                        UserType = string.IsNullOrEmpty(listUserType) ? UserType.Client.ToString() : listUserType
                    };
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
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        if (Session["UserType"] != null && (string)Session["UserType"] != UserType.Sale.ToString())
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

                            return RedirectToAction("Manage", "Account");

                        }
                        else
                        {
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

            ViewBag.ListStatus = GetListStatus(listStatus);
            List<Order> listOrder = new List<Order>();

            if ((string)Session["UserType"] == UserType.Sale.ToString())
            {
                var listUserManage = db.SaleManageClients.Where(a => a.User_Sale == User.Identity.Name).Select(a => a.User_Client).ToList();
                listOrder = db.Orders.Where(a => listUserManage.Contains(a.Phone) || string.IsNullOrEmpty(a.SaleManager)).ToList();
            }
            else if ((string)Session["UserType"] == UserType.Client.ToString())
            {
                listOrder = db.Orders.Where(a => a.Phone == User.Identity.Name).ToList();
            }
            else if ((string)Session["UserType"] == UserType.Accounting.ToString())
            {
                listOrder = db.Orders.Where(a => a.Status == OrderStatus.SaleConfirm.ToString()).ToList();
            }
            else if ((string)Session["UserType"] == UserType.Orderer.ToString())
            {
                listOrder = db.Orders.Where(a => a.Status == OrderStatus.Paid.ToString()).ToList();
            }
            else if ((string)Session["UserType"] == UserType.Recieve.ToString())
            {
                listOrder = db.Orders.Where(a => a.Status == OrderStatus.Order.ToString() || a.Status == OrderStatus.Receive.ToString()).ToList();
            }
            else if ((string)Session["UserType"] == UserType.Admin.ToString() || (string)Session["UserType"] == UserType.SuperUser.ToString())
            {
                listOrder = db.Orders.ToList();
            }
            if (!string.IsNullOrEmpty(username))
            {
                listOrder = listOrder.FindAll(a => a.Phone.IndexOf(username, System.StringComparison.Ordinal) > 0);
            }

            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                listOrder = (from order in listOrder
                             where fromdate <= order.CreateDate && order.CreateDate <= todate
                             select order).ToList();
            }

            if (!string.IsNullOrEmpty(listStatus))
            {
                listOrder = listOrder.FindAll(a => a.Status == listStatus);
            }
            if (!string.IsNullOrEmpty(OrderId))
            {
                listOrder = listOrder.FindAll(a => a.OrderId.ToString() == OrderId);
            }

            listOrder = listOrder.OrderByDescending(a => a.CreateDate).ToList();

            ViewBag.CurrentToDate = toDate;
            ViewBag.CurrentFromDate = fromDate;
            ViewBag.CurrentStatus = listStatus;
            ViewBag.CurrentOrderId = OrderId;
            ViewBag.CurrentUserName = username;


            const int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(listOrder.ToPagedList(pageNumber, pageSize));
        }

        private SelectList GetListStatus(string value)
        {
            List<Object> list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(OrderStatus).GetFields())
            {
                if (fieldInfo.FieldType.Name != "OrderStatus")
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                if (attribute != null)
                    list.Add(new { name = fieldInfo.Name, display = attribute.Name });
                else
                    list.Add(new { name = fieldInfo.Name, display = fieldInfo.Name });

            }

            return new SelectList(list, "name", "display", value);
        }

        private SelectList GetListUserType(string value = "")
        {
            List<Object> list = new List<object>();
            foreach (FieldInfo fieldInfo in typeof(UserType).GetFields())
            {
                if (fieldInfo.FieldType.Name != "UserType")
                    continue;
                var attribute = Attribute.GetCustomAttribute(fieldInfo,
                   typeof(DisplayAttribute)) as DisplayAttribute;

                if (attribute != null)
                    list.Add(new { name = fieldInfo.Name, display = attribute.Name });
                else
                    list.Add(new { name = fieldInfo.Name, display = fieldInfo.Name });

            }

            return new SelectList(list, "name", "display", value);
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
                            TotalPrice = model.ListOrderDetail.Sum(a => a.Price ?? 0),
                            Status = OrderStatus.New.ToString(),
                            Rate = rate != null ? rate.Price : 0,
                            Phone = model.ListOrderDetail.First().Phone
                        };

                        db.Entry(order).State = EntityState.Added;
                        db.Orders.Add(order);
                        db.SaveChanges();

                        int orderId = order.OrderId;

                        foreach (var orderDetail in model.ListOrderDetail)
                        {
                            orderDetail.OrderId = orderId;
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

        //private Stream GenerateStreamFromString(string s)
        //{
        //    MemoryStream stream = new MemoryStream();
        //    StreamWriter writer = new StreamWriter(stream);
        //    writer.Write(s);
        //    writer.Flush();
        //    stream.Position = 0;
        //    return stream;
        //}


        public ActionResult ViewOrderDetail(int id, string message)
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
            ViewData["message"] = message;
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
                order.Status = OrderStatus.SaleConfirm.ToString();
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
        public ActionResult OrdererRejectOrder(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString(CultureInfo.InvariantCulture) == orderid);
            if (model != null)
            {
                model.Status = OrderStatus.ClientConfirm.ToString();
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult OrdererConfirmOrder(string orderid)
        {
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString(CultureInfo.InvariantCulture) == orderid);
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
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString(CultureInfo.InvariantCulture) == orderid);
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
            var model = db.Orders.FirstOrDefault(a => a.OrderId.ToString(CultureInfo.InvariantCulture) == orderid);
            if (model != null)
            {

                model.Status = OrderStatus.Finish.ToString();
                db.SaveChanges();

                return Json(new { success = true });
            }
            return Json(new { success = false });
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
                    order.Status = OrderStatus.Paid.ToString();

                }
                else if (model.Status == OrderStatus.Receive.ToString())
                {
                    order.AccountingCollected = model.AccountingCollected;
                    order.Status = OrderStatus.FullCollect.ToString();
                }
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
                        db.OrderDetails.Add(model);
                        db.SaveChanges();

                        //tính toán lại order
                        var ord = db.Orders.FirstOrDefault(a => a.OrderId == model.OrderId);
                        if (ord != null)
                        {
                            ord.TotalPrice =
                                db.OrderDetails.Where(a => a.OrderId == model.OrderId).Sum(a => a.Quantity * a.Price ?? 0);
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
                            db.SaveChanges();

                            var ord = db.Orders.FirstOrDefault(a => a.OrderId == model.OrderId);
                            if (ord != null)
                            {
                                ord.TotalPrice =
                                    db.OrderDetails.Where(a => a.OrderId == model.OrderId)
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
                db.OrderDetails.Remove(orderDetail);
                db.SaveChanges();

                var ord = db.Orders.FirstOrDefault(a => a.OrderId == orderDetail.OrderId);
                if (ord != null)
                {
                    ord.TotalPrice = db.OrderDetails.Where(a => a.OrderId == orderDetail.OrderId).Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0));
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
            var order = db.Orders.FirstOrDefault(m => m.OrderId.ToString(CultureInfo.InvariantCulture) == orderId);
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
            var order = db.Orders.FirstOrDefault(m => m.OrderId.ToString(CultureInfo.InvariantCulture) == orderId);
            if (order != null)
            {
                order.Status = OrderStatus.ClientConfirm.ToString();
                db.SaveChanges();
            }
            return Json(new { success = true });
        }
        #endregion

        #region Administrator

        public ActionResult ListClient(string userType, string userName, int? page)
        {
            ViewBag.ListUserType = GetListUserType(userType);
            var userProfiles = new List<UserProfile>();

            userProfiles = !string.IsNullOrEmpty(userType) ? db.UserProfiles.Where(a => a.UserType == userType).ToList() : db.UserProfiles.ToList();
            if (!string.IsNullOrEmpty(userName))
            {
                userProfiles = userProfiles.FindAll(a => !String.Equals(a.Phone, User.Identity.Name, StringComparison.CurrentCultureIgnoreCase) && a.Phone.ToLower().Contains(userName.ToLower()));
            }
            foreach (var userProfile in userProfiles)
            {
                if (userProfile.UserType == UserType.Client.ToString())
                {
                    //get sale
                    var salemanage =
                        db.SaleManageClients.FirstOrDefault(a => a.User_Client == userProfile.Phone);
                    if (salemanage != null)
                    {
                        userProfile.SaleManage = salemanage.User_Sale;
                    }
                }
            }
            ViewBag.CurrentUserType = userType;
            ViewBag.CurrentUserName = userName;

            const int pageSize = 5;
            int pageNumber = (page ?? 1);

            return View(userProfiles.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult AssignSaleForClient(string id, string userSale)
        {
            var model = new SaleManageClient { User_Client = id, User_Sale = userSale };
            var listSale = db.UserProfiles.Where(a => a.UserType == UserType.Sale.ToString()).Select(a => new { phone = a.Phone, display = a.Name });
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
            var model = db.UserProfiles.FirstOrDefault(a => a.Phone == id);
            if (model == null)
                return RedirectToAction("ListClient");

            ViewBag.listUserType = GetListUserType(userType);

            return PartialView("_ChangeUserTypePartial", model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeUserType(UserProfile model)
        {
            var userUpdate = db.UserProfiles.FirstOrDefault(a => a.Phone == model.Phone);
            if (userUpdate != null)
            {
                userUpdate.UserType = model.UserType;
                db.SaveChanges();

            }
            return RedirectToAction("ListClient");
        }

        public ActionResult UpdateRate(string fromDate, string toDate, int? page)
        {
            const int pageSize = 5;
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
        public ActionResult ChangePass(int id, FormCollection collection)
        {
            if (ModelState.IsValid)
            {
                var user = db.UserProfiles.First(m => m.UserId == id);
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
