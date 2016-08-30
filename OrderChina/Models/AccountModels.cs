using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Reflection;
using System.Web.Mvc;
using System.Web.UI.WebControls.Expressions;

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
    }

    [Table("UserProfile")]
    public class UserProfile
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string Phone { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime Birthday { get; set; }
        public string Address { get; set; }
        public string Account { get; set; }
        public string Gender { get; set; }
        public string Password { get; set; }

        public string UserType { get; set; }
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
        public string SellManager { get; set; }

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

        public RateHistory CloneHistory()
        {
            return new RateHistory
            {
                Price = Price,
                fee1 = fee1,
                fee2 = fee2,
                fee3 = fee3,
                LastUpdate = DateTime.Now
            };
        }

    }
    [Table("RateHistory")]

    public class RateHistory
    {
        [Key]
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int RateHistoryId { get; set; }

        public double Price { get; set; }

        public double fee1 { get; set; }
        public double fee2 { get; set; }
        public double fee3 { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UserUpdate { get; set; }
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

        [Required]
        [Remote("IsCheckEmail", "Account", ErrorMessage = "Định dạng email không đúng.")]
        [Display(Name = "Email *")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu *")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nhập lại mật khẩu *")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Họ tên *")]
        public string Name { get; set; }

        [Required]
        [Remote("IsCheckExitsPhone", "Account", ErrorMessage = "Số điện thoại {0} đã tồn tại trong hệ thống.")]
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
        public string SellManager { get; set; }

        public IEnumerable<OrderDetail> ListOrderDetails { get; set; }
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
        [Display(Name = "Nhân viên sell")]
        Seller = 3,
        [Display(Name = "Kế toán")]
        Accounting = 4,
        [Display(Name = "Khách hàng")]
        Client = 5
    }
}
