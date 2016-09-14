using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using OrderChina.Models;

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
    }
}
