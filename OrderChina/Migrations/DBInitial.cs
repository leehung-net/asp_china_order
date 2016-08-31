using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using OrderChina.Models;

namespace OrderChina.Migrations
{
    public class DBInitialize : DropCreateDatabaseIfModelChanges<DBContext>
    {
        protected override void Seed(DBContext context)
        {
            context.UserProfiles.Add(new UserProfile
            {
                Email = "admin@gmail.com",
                Name = "Lê Văn Hùng",
                UserType = UserType.Admin.ToString(),
                Phone = "0977261061",
                Password = "123456",
                Address = "Hoài đức",
                Account = "123456789",
                Gender = "Nam",
                Birthday = DateTime.Now
            });

            context.Rates.Add(new Rate
            {
                fee1 = 20000,
                fee2 = 24000,
                fee3 = 28000,
                Price = 3200,
                userUpdate = "admin@gmail.com"
            });
        }
    }
}