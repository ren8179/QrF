namespace QrF.Account.EntityFramework
{
    using QrF.Framework.Utility;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<AccountDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }

        protected override void Seed(AccountDbContext context)
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
            //context.Roles.AddOrUpdate(new Contract.Role { Name = "系统管理员", CreateTime = DateTime.Now });
            //context.Users.AddOrUpdate(new Contract.User { LoginName = "admin", Password = Encrypt.MD5("admin258"), CreateTime=DateTime.Now, UserName="系统管理员" });
        }
    }
}
