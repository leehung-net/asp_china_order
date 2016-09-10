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
                userUpdate = "admin@gmail.com",
                lastUpdated = DateTime.Now
            });

            context.Currencys.Add(new Currency
            {
                Code = "AUD",
                Description = "Australia Dollar"
            });
            context.Currencys.Add(new Currency
            {
                Code = "CAD",
                Description = "Canada Dollar"
            }); context.Currencys.Add(new Currency
            {
                Code = "CNY",
                Description = "China Yuan Renminbi"
            });
            context.Currencys.Add(new Currency
            {
                Code = "EUR",
                Description = "Euro Member Countries"
            }); context.Currencys.Add(new Currency
            {
                Code = "HKD",
                Description = "Hong Kong Dollar"
            }); context.Currencys.Add(new Currency
            {
                Code = "JPY",
                Description = "Japan Yen"
            }); context.Currencys.Add(new Currency
            {
                Code = "RUB",
                Description = "Russia Ruble"
            });
            context.Currencys.Add(new Currency
            {
                Code = "THB",
                Description = "Thailand Baht"
            }); context.Currencys.Add(new Currency
            {
                Code = "USD",
                Description = "United States Dollar"
            }); context.Currencys.Add(new Currency
            {
                Code = "VND",
                Description = "Viet Nam Dong"
            });

            //Australia Dollar	AUD		
            //Canada Dollar	CAD		
            //China Yuan Renminbi	CNY	
            //Euro Member Countries	EUR	
            //Hong Kong Dollar	HKD	
            //Japan Yen	JPY		
            //Russia Ruble	RUB		
            //Thailand Baht	THB	
            //United States Dollar	USD	
            //Viet Nam Dong	VND	 
        }
    }
}