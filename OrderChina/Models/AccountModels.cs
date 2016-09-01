using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Reflection;
using System.Web.Mvc;

namespace OrderChina.Models
{
    public class DBContext : DbContext
    {
        #region Database
        public DBContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        public DbSet<Rate> Rates { get; set; }

        public DbSet<RateHistory> RateHistorys { get; set; }
        public DbSet<SaleManageClient> SaleManageClients { get; set; }
    }

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string Phone { get; set; }

        public string PhoneCompany { get; set; }

        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Birthday { get; set; }
        public string Address { get; set; }
        public string Account { get; set; }
        public string Gender { get; set; }
        public string Password { get; set; }

        public string UserType { get; set; }

        [NotMapped]
        public string SaleManage { get; set; }

        public string getUserTypeText()
        {
            foreach (FieldInfo fieldInfo in typeof(UserType).GetFields())
            {
                if (fieldInfo.FieldType.Name != "UserType")
                    continue;
                if (String.Equals(fieldInfo.Name, UserType, StringComparison.CurrentCultureIgnoreCase))
                {
                    var attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DisplayAttribute)) as DisplayAttribute;

                    if (attribute != null)
                        return attribute.Name;
                }

            }

            return UserType;
        }
    }

    [Table("Order")]

    public class Order
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        [Display(Name = "Mã đơn hàng")]
        public int OrderId { get; set; }


        public string UserName { get; set; }
        public string Phone { get; set; }
        [Display(Name = "Tỷ giá")]
        public double Rate { get; set; }
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }
        public double TotalPrice { get; set; }
        [Display(Name = "Tổng tiền")]
        public double TotalPriceConvert { get; set; }

        public double Fee { get; set; }

        public double FeeShipChina { get; set; }

        public int Weight { get; set; }

        public double FeeShip { get; set; }

        [Display(Name = "Ngày tạo")]
        public DateTime CreateDate { get; set; }
        public string SaleManager { get; set; }

        public string getStatusText()
        {
            foreach (FieldInfo fieldInfo in typeof(OrderStatus).GetFields())
            {
                if (fieldInfo.FieldType.Name != "OrderStatus")
                    continue;
                if (fieldInfo.Name.ToLower() == Status.ToLower())
                {
                    var attribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DisplayAttribute)) as DisplayAttribute;

                    if (attribute != null)
                        return attribute.Name;
                }

            }

            return Status;
        }
    }

    [Table("OrderDetail")]
    public class OrderDetail
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }

        [Required]
        public string Link { get; set; }
        public string Shop { get; set; }
        public string Description { get; set; }

        [Required]
        public int? Quantity { get; set; }

        [Required]
        public double? Price { get; set; }

        [Required]
        public string Phone { get; set; }

        public int QuantitySellPlace { get; set; }

        public int QuantityInWarehouse { get; set; }

        public string Note { get; set; }

    }

    [Table("Rate")]
    public class Rate
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RateId { get; set; }

        public double Price { get; set; }

        public double fee1 { get; set; }
        public double fee2 { get; set; }
        public double fee3 { get; set; }

        public string userUpdate { get; set; }
        public DateTime lastUpdated { get; set; }

        public RateHistory CloneHistory()
        {
            return new RateHistory
            {
                RateId = RateId,
                Price = Price,
                fee1 = fee1,
                fee2 = fee2,
                fee3 = fee3,
                UserUpdate = userUpdate,
                LastUpdate = lastUpdated
            };
        }

        public string FormatPrice(double index)
        {

            int sp = 1000;
            string hq = "k";
            if (1000 <= index && index < 1000000 || -1000 >= index && index > -1000000)
            {
                sp = 1000;
                hq = "k";
            }
            if (1000000 <= index && index < 1000000000 || -1000000 >= index && index > -1000000000)
            {
                sp = 1000000;
                hq = "m";
            }
            if (index >= 1000000000 || index <= -1000000000)
            {
                sp = 1000000000;
                hq = "b";
            }

            if (index >= 1000 || index <= -1000)
            {
                var pt = index;
                if (index < 0)
                {
                    pt = pt * (-1);
                }

                var p = Math.Round((float)pt / sp, 1);
                var txt = p.ToString(CultureInfo.InvariantCulture);

                if (p - (int)p == 0)
                {
                    if (index > 0)
                    {
                        return txt + hq;
                    }
                    return "-" + txt + hq;
                }
                else
                {
                    if (index > 0)
                    {
                        return txt.Replace(".", hq);
                    }
                    return "-" + txt.Replace(".", hq);
                }
            }

            return index.ToString();
        }
    }

    [Table("RateHistory")]
    public class RateHistory
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RateHistoryId { get; set; }
        public int RateId { get; set; }

        public double Price { get; set; }

        public double fee1 { get; set; }
        public double fee2 { get; set; }
        public double fee3 { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UserUpdate { get; set; }

    }

    [Table("SaleManageClient")]
    public class SaleManageClient
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Phải chọn sale")]
        [Display(Name = "Sale quản lý")]
        public string User_Sale { get; set; }

        public string User_Client { get; set; }
        public string User_Update { get; set; }
        public DateTime LastUpdate { get; set; }

    }
        #endregion

    #region User
    public class LoginModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }

        [Display(Name = "Nhớ tài khoản?")]
        public bool RememberMe { get; set; }

    }

    public class RegisterModel
    {

        public string UserName { get; set; }

        [Required(ErrorMessage = "Email bắt buộc nhập.")]
        [Remote("IsCheckEmail", "Account", "")]
        [Display(Name = "Email *")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mật khẩu bắt buộc nhập.")]
        [StringLength(100, ErrorMessage = "{0} phải lớn hơn {2} ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu *")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu *")]
        [Compare("Password", ErrorMessage = "Mật khẩu không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Họ tên bắt buộc nhập.")]
        [Display(Name = "Họ tên *")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sđt bắt buộc nhập.")]
        [Remote("IsCheckExitsPhone", "Account", ErrorMessage = "Sđt {0} đã tồn tại trong hệ thống.")]
        [Display(Name = "Số điện thoại *")]
        public string Phone { get; set; }

        [Required]
        [Display(Name = "Giới tính *")]
        public string Gender { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày sinh *")]
        public DateTime Birthday { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "TK ngân hàng")]
        public string Account { get; set; }

        [Display(Name = "Loại tài khoản")]
        public string UserType { get; set; }
    }


    public class ExternalLogin
    {
        public string Provider { get; set; }
        public string ProviderDisplayName { get; set; }
        public string ProviderUserId { get; set; }
    }

    public class LocalPasswordModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
    #endregion

    #region Order
    public class NewOrderModel
    {
        public IEnumerable<OrderDetail> ListOrderDetail { get; set; }
    }

    public class ViewDetailOrderModel
    {
        public int OrderId { get; set; }

        public string UserName { get; set; }
        public string Phone { get; set; }
        [Display(Name = "Tỷ giá")]
        public double Rate { get; set; }
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }
        public double TotalPrice { get; set; }
        [Display(Name = "Tổng tiền")]
        public double TotalPriceConvert { get; set; }

        public double Fee { get; set; }

        public double FeeShipChina { get; set; }

        public int Weight { get; set; }

        public double FeeShip { get; set; }

        public DateTime CreateDate { get; set; }
        public string SaleManager { get; set; }

        public SaleManageInfo SaleManageInfo { get; set; }
        public IEnumerable<OrderDetail> ListOrderDetails { get; set; }
    }

    public class SaleManageInfo
    {
        public string SaleName { get; set; }

        public string SalePhoneCompany { get; set; }
        public string SalePhone { get; set; }
    }
    #endregion

    #region Accounting

    public class RateModel
    {
        public Rate Rate { get; set; }

        public PagedList.IPagedList<RateHistory> ListRateHistory { get; set; }
    }
    #endregion

    public enum OrderStatus
    {
        [Display(Name = "Đơn mới")]
        New = 1,
        [Display(Name = "Đã thu tiền")]
        Levy = 2,
        [Display(Name = "Đặt hàng")]
        Order = 3,
        [Display(Name = "Hoàn thành")]
        Finish = 4,
        [Display(Name = "Đơn hủy")]
        Cancel = 5
    }

    public enum UserType
    {
        [Display(Name = "Administrator")]
        Admin = 1,
        [Display(Name = "SupperUser")]
        SuperUser = 2,
        [Display(Name = "Nhân viên sale")]
        Sale = 3,
        [Display(Name = "Kế toán")]
        Accounting = 4,
        [Display(Name = "Khách hàng")]
        Client = 5,
        [Display(Name = "Nhân viên đặt hàng")]
        Orderer = 6
    }
}
