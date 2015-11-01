using System;

namespace QrF.Core.Config
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    [Serializable]
    public class DaoConfig : ConfigFileBase
    {
        public DaoConfig()
        {
        }
        #region 序列化属性
        public string Account { get; set; }
        public string Log { get; set; }
        #endregion
    }
}
