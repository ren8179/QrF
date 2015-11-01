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
        public String Account { get; set; }
        public String Log { get; set; }
        public string Work { get; set; }
        #endregion
    }
}
