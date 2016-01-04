using System.Collections.Generic;
using System.Data;

namespace QrF.Framework.Utility
{
    public static class DataTableHelper
    {
        /// <summary> 
        /// DataTable转为List数据  
        /// </summary> 
        /// <param name="dt">DataTable</param> 
        /// <returns>List数据</returns> 
        public static List<object> ToObjectList(this DataTable dt)
        {
            List<object> dic = new List<object>();

            foreach (DataRow dr in dt.Rows)
            {
                Dictionary<string, object> result = new Dictionary<string, object>();

                foreach (DataColumn dc in dt.Columns)
                {
                    result.Add(dc.ColumnName, dr[dc].ToString());
                }
                dic.Add(result);
            }
            return dic;
        }
    }
}
