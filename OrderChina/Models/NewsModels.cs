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
    public class NewsModel : DbContext
    {
        #region Database
        public NewsModel()
            : base("DefaultConnection")
        {
        }
        //public DbSet<News> News { get; set; }

    }
    //[Table("News")]
    //public class News
    //{
    //    [Key]
    //    [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
    //    public int IDNews { get; set; }
    //    public string Title { get; set; }
    //    public string NewsContent { get; set; }
    //    public string Titlebig { get; set; }
    //    public string Img { get; set; }

    //}
        #endregion
}