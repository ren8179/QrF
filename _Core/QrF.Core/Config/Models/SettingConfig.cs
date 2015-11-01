using System;

namespace QrF.Core.Config
{
    /// <summary>
    /// 数据库配置
    /// </summary>
    [Serializable]
    public class SettingConfig : ConfigFileBase
    {
        public SettingConfig()
        {
        }

        #region 序列化属性
        public int UserLoginTimeoutMinutes { get; set; }
        public string WebSiteTitle { get; set; }
        public string WebSiteDescription { get; set; }
        public string WebSiteKeywords { get; set; }
        #endregion
    }
}
