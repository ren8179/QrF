using QrF.Sqlite.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QrF.Sqlite.Service
{
    public interface ISqliteService
    {
        #region Customer
        /// <summary>
        /// 查询单个对象
        /// </summary>
        Customer GetCustomer(int id);
        /// <summary>
        /// 查询列表(未分页)
        /// </summary>
        IEnumerable<Customer> GetCustomerList(CustomerRequest request = null);
        /// <summary>
        /// 查询列表(分页)
        /// </summary>
        IEnumerable<Customer> GetCustomerPageList(CustomerRequest request = null);
        /// <summary>
        /// 编辑保存
        /// </summary>
        void SaveCustomer(Customer model);
        /// <summary>
        /// 删除
        /// </summary>
        void DeleteCustomer(List<int> ids);
        
        #endregion

    }
}
