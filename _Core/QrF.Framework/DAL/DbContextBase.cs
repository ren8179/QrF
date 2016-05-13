using QrF.Framework.Contract;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;

namespace QrF.Framework.DAL
{
    /// <summary>
    /// DAL基类，实现Repository通用泛型数据访问模式
    /// </summary>
    public class DbContextBase : DbContext, IDataRepository, IDisposable
    {
        public DbContextBase(string connectionString)
        {
            this.Database.Connection.ConnectionString = connectionString;
            this.Configuration.LazyLoadingEnabled = false;
            this.Configuration.ProxyCreationEnabled = false;
        }

        public DbContextBase(string connectionString, IAuditable auditLogger)
            : this(connectionString)
        {
            this.AuditLogger = auditLogger;
        }

        public IAuditable AuditLogger { get; set; }

        public T Update<T>(T entity) where T : ModelBase
        {
            var set = this.Set<T>();
            set.Attach(entity);
            this.Entry<T>(entity).State = EntityState.Modified;
            this.SaveChanges();
            return entity;
        }

        public async Task<T> UpdateAsync<T>(T entity) where T : class, new()
        {
            var set = this.Set<T>();
            set.Attach(entity);
            this.Entry<T>(entity).State = EntityState.Modified;
            await base.SaveChangesAsync();
            return entity;
        }

        public T Insert<T>(T entity) where T : ModelBase
        {
            this.Set<T>().Add(entity);
            this.SaveChanges();
            return entity;
        }

        public async Task<T> InsertAsync<T>(T entity) where T : class, new ()
        {
            this.Set<T>().Add(entity);
            await base.SaveChangesAsync();
            return entity;
        }

        public void Delete<T>(T entity) where T : ModelBase
        {
            this.Entry<T>(entity).State = EntityState.Deleted;
            this.SaveChanges();
        }

        public async Task DeleteAsync<T>(T entity) where T :class, new()
        {
            this.Entry<T>(entity).State = EntityState.Deleted;
            await base.SaveChangesAsync();
        }

        public T Find<T>(params object[] keyValues) where T : ModelBase
        {
            return this.Set<T>().Find(keyValues);
        }

        public List<T> FindAll<T>(Expression<Func<T, bool>> conditions = null) where T : ModelBase
        {
            if (conditions == null)
                return this.Set<T>().ToList();
            else
                return this.Set<T>().Where(conditions).ToList();
        }

        public PagedList<T> FindAllByPage<T, S>(Expression<Func<T, bool>> conditions, Expression<Func<T, S>> orderBy, int pageSize, int pageIndex) where T : ModelBase
        {
            var queryList = conditions == null ? this.Set<T>() : this.Set<T>().Where(conditions) as IQueryable<T>;

            return queryList.OrderByDescending(orderBy).ToPagedList(pageIndex, pageSize);
        }

        public override int SaveChanges()
        {
            this.WriteAuditLog();

            var result = base.SaveChanges();
            return result;
        }

        internal void WriteAuditLog()
        {
            if (this.AuditLogger == null)
                return;

            foreach (var dbEntry in this.ChangeTracker.Entries<ModelBase>().Where(p => p.State == EntityState.Added || p.State == EntityState.Deleted || p.State == EntityState.Modified))
            {
                var auditableAttr = dbEntry.Entity.GetType().GetCustomAttributes(typeof(AuditableAttribute), false).SingleOrDefault() as AuditableAttribute;
                if (auditableAttr == null)
                    continue;

                var context = CallContext.HostContext as System.Web.HttpContext;
                var operaterName = context == null ? WCFContext.Current.Operater.Name : context.User.Identity.Name;

                Task.Factory.StartNew(() =>
                {
                    var tableAttr = dbEntry.Entity.GetType().GetCustomAttributes(typeof(TableAttribute), false).SingleOrDefault() as TableAttribute;
                    string tableName = tableAttr != null ? tableAttr.Name : dbEntry.Entity.GetType().Name;
                    var moduleName = dbEntry.Entity.GetType().FullName.Split('.').Skip(1).FirstOrDefault();

                    this.AuditLogger.WriteLog(dbEntry.Entity.ID, operaterName, moduleName, tableName, dbEntry.State.ToString(), dbEntry.Entity);
                });
            }

        }
    }
}
