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
            } );
            context.UserProfiles.Add(new UserProfile
            {
                Email = "Accounting@gmail.com",
                Name = "Accounting",
                UserType = UserType.Accounting.ToString(),
                Phone = "0977261065",
                Password = "123456",
                Address = "Hoài đức",
                Account = "123456789",
                Gender = "Nam",
                Birthday = DateTime.Now
            });
            context.UserProfiles.Add(new UserProfile
            {
                Email = "client@gmail.com",
                Name = "client",
                UserType = UserType.Client.ToString(),
                Phone = "0977261062",
                Password = "123456",
                Address = "Hoài đức",
                Account = "123456789",
                Gender = "Nam",
                Birthday = DateTime.Now
            });
            context.UserProfiles.Add(new UserProfile
            {
                Email = "orderer@gmail.com",
                Name = "Orderer",
                UserType = UserType.Orderer.ToString(),
                Phone = "0977261063",
                Password = "123456",
                Address = "Hoài đức",
                Account = "123456789",
                Gender = "Nam",
                Birthday = DateTime.Now
            });
            context.UserProfiles.Add(new UserProfile
            {
                Email = "sale@gmail.com",
                Name = "Sale",
                UserType = UserType.Sale.ToString(),
                Phone = "0977261064",
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