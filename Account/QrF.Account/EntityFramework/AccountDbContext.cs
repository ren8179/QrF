﻿using QrF.Account.Contract;
using QrF.Core.Config;
using QrF.Framework.DAL;
using System;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using System.Reflection;

namespace QrF.Account.EntityFramework
{
    public class AccountDbContext : DbContextBase
    {
        public AccountDbContext()
            //: base(@"Data Source=WIN-NRSLQON20B9\SQLEXPRESS;Initial Catalog=QrF.Account;Persist Security Info=True;User ID=sa;Password=pass", new LogDbContext()){}
            : base(CachedConfigContext.Current.DaoConfig.Account, new LogDbContext()){}
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            Database.SetInitializer<AccountDbContext>(null);
            var typesToRegister = Assembly.GetExecutingAssembly().GetTypes()
                    .Where(type => !String.IsNullOrEmpty(type.Namespace))
                    .Where(type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>));
            foreach (var type in typesToRegister)
            {
                dynamic configurationInstance = Activator.CreateInstance(type);
                modelBuilder.Configurations.Add(configurationInstance);
            }
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<LoginInfo> LoginInfos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Menu> Menus { get; set; }
        public DbSet<VerifyCode> VerifyCodes { get; set; }
    }
}
