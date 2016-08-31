using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web.DynamicData;
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
        DBContext db = new DBContext();

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
            if (IsValid(model.Email, model.Password))
            {
                FormsAuthentication.SetAuthCookie(model.Email, false);
                return RedirectToLocal(returnUrl);
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


        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            WebSecurity.Logout();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register()
        {
            var model = new RegisterModel();
            model.Birthday = DateTime.Now;
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

                    if (!Request.IsAuthenticated)
                    {
                        FormsAuthentication.SetAuthCookie(model.Email, false);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        return RedirectToAction("ListClient", "Account");
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

        public ActionResult Manage(string listStatus, string fromDate, string toDate, string OrderId, int? page)
        {

            ViewBag.ListStatus = GetListStatus(listStatus);
            List<Order> listOrder = new List<Order>();
            if (!string.IsNullOrEmpty(fromDate) && !string.IsNullOrEmpty(toDate))
            {
                var fromdate = DateTime.ParseExact(fromDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                var todate = DateTime.ParseExact(toDate, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                listOrder = (from order in db.Orders
                             where fromdate <= order.CreateDate && order.CreateDate <= todate
                             select order).ToList();
            }
            else
            {
                listOrder = db.Orders.ToList();
            }
            if (!string.IsNullOrEmpty(listStatus))
            {
                listOrder = listOrder.FindAll(a => a.Status == listStatus);
            }
            if (!string.IsNullOrEmpty(OrderId))
            {
                listOrder = listOrder.FindAll(a => a.OrderId.ToString() == OrderId);
            }

            if (Request.IsAuthenticated)
            {
                listOrder = listOrder.FindAll(a => a.UserName == User.Identity.Name);
            }

            ViewBag.CurrentToDate = toDate;
            ViewBag.CurrentFromDate = fromDate;
            ViewBag.CurrentStatus = listStatus;
            ViewBag.CurrentOrderId = OrderId;

            const int pageSize = 3;
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
        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            // If we got this far, something failed, redisplay form
            return View();
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
                        var user = db.UserProfiles.First(a => a.Email == User.Identity.Name);
                        var order = new Order
                        {
                            CreateDate = DateTime.Now,
                            TotalPrice = model.ListOrderDetail.Sum(a => (a.Quantity ?? 0) * (a.Price ?? 0)),
                            Status = OrderStatus.New.ToString(),
                            Rate = rate != null ? rate.Price : 0,
                            Phone = user.Phone,
                            UserName = user.Email
                        };
                        order.TotalPriceConvert = order.TotalPrice * order.Rate;
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
        #endregion

        public ActionResult ViewOrderDetail(int id, string message)
        {
            ViewDetailOrderModel model = new ViewDetailOrderModel();
            var order = db.Orders.FirstOrDefault(a => a.OrderId == id);
            var orderDetail = db.OrderDetails.Where(a => a.OrderId == id);
            model.OrderId = id;
            if (order != null)
            {
                model.Phone = order.Phone;
                model.Rate = order.Rate;
                model.Status = order.getStatusText();
                model.TotalPriceConvert = order.TotalPriceConvert;
                model.TotalPrice = order.TotalPrice;
                model.SellManager = order.SellManager;
                model.UserName = order.UserName;
                model.FeeShip = order.FeeShip;
                model.FeeShipChina = order.FeeShipChina;
                model.CreateDate = order.CreateDate;
                model.Fee = order.Fee;
                model.Weight = order.Weight;
            }
            model.ListOrderDetails = orderDetail;
            ViewData["message"] = message;
            return View(model);
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
                        var user = db.UserProfiles.FirstOrDefault(a => a.Email == User.Identity.Name);
                        if (user != null) model.Phone = user.Phone;
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

        public ActionResult ListClient(string userType, string userName, int? page)
        {
            ViewBag.ListUserType = GetListUserType(userType);
            var userProfiles = new List<UserProfile>();

            userProfiles = !string.IsNullOrEmpty(userType) ? db.UserProfiles.Where(a => a.UserType == userType).ToList() : db.UserProfiles.ToList();
            if (!string.IsNullOrEmpty(userName))
            {
                userProfiles = userProfiles.FindAll(a => !String.Equals(a.Email, User.Identity.Name, StringComparison.CurrentCultureIgnoreCase) && a.Email.ToLower().Contains(userName.ToLower()));
            }
            foreach (var userProfile in userProfiles)
            {
                if (userProfile.UserType == UserType.Client.ToString())
                {
                    //get sale
                    var salemanage =
                        db.SaleManageClients.FirstOrDefault(a => a.User_Client == userProfile.Email);
                    if (salemanage != null)
                    {
                        userProfile.SaleManage = salemanage.User_Sale;
                    }
                }
            }
            ViewBag.CurrentUserType = userType;
            ViewBag.CurrentUserName = userName;

            const int pageSize = 3;
            int pageNumber = (page ?? 1);

            return View(userProfiles.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult AssignSaleForClient(string id, string userSale)
        {
            var model = new SaleManageClient { User_Client = id, User_Sale = userSale };
            var listSale = db.UserProfiles.Where(a => a.UserType == UserType.Sale.ToString()).Select(a => new { email = a.Email, display = a.Name });
            ViewBag.listSale = new SelectList(listSale, "email", "display", userSale);
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
                model.User_Update = User.Identity.Name;
                model.LastUpdate = DateTime.Now;
                db.SaleManageClients.Add(model);
                db.SaveChanges();
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

        public ActionResult ChangeUserType(string id,string userType)
        {
            var model = db.UserProfiles.FirstOrDefault(a => a.Email == id);
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
            var userUpdate = db.UserProfiles.FirstOrDefault(a => a.Email == model.Email);
            if (userUpdate != null)
            {
                userUpdate.UserType = model.UserType;
                db.SaveChanges();

            }
            return RedirectToAction("ListClient");
        }

    }

}
