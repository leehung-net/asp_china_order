using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Web;
using System.Web.UI.WebControls;
using OrderChina.Models;

namespace OrderChina.Common
{
    public class Utilities
    {
        public static bool CheckRole(string userType, string roleCheck = "")
        {
            
            if (userType == UserType.Admin.ToString() || userType == UserType.SuperUser.ToString())
                return true;
            if (userType == roleCheck)
                return true;

            return false;
        }
    }
}