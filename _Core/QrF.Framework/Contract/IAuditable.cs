
namespace QrF.Framework.Contract
{
    /// <summary>
    /// 用于写数据修改，添加等历史日志
    /// </summary>
    public interface IAuditable
    {
        void WriteLog(int modelId, string userName, string moduleName, string tableName, string eventType, ModelBase newValues);
        void WriteLog(int modelId, string userName, string moduleName, string tableName, string eventType, string newValues);
    }
}
