using DotNetOpenAuth.OpenId.Extensions.AttributeExchange;
using OrderChina.Models;

namespace OrderChina.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<OrderChina.Models.DBContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(OrderChina.Models.DBContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method 
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //
            context.UserProfiles.AddOrUpdate(new UserProfile
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

            context.Rates.AddOrUpdate(new Rate
            {
                fee1 = 20000,
                fee2 = 24000,
                fee3 = 28000,
                Price = 3200
            });
        }
    }
}
