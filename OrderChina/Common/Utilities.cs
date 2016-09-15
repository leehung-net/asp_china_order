using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.UI.WebControls;
using OrderChina.Models;

namespace OrderChina.Common
{
    public class Utilities
    {
        public static bool CheckRole(string userType, int roleCheck = 0,bool isAdmin = true)
        {
            if (userType == null)
            {
                return false;
            }
            if (isAdmin)
            {
                if (userType.IndexOf(((int)UserType.Admin).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
                if (userType.IndexOf(((int)UserType.SuperUser).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
           
            if (userType.IndexOf(roleCheck.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal) >= 0)
                return true;

            return false;
        }

        public static string GenerateOrderId(int id, int saleId, int userid)
        {
            return string.Format("{0:ddMMyy}{1}{2}{3}", DateTime.Now, saleId, userid, id);
        }
    }
}