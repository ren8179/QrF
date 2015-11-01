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
        public String WebSiteTitle { get; set; }
        public String WebSiteDescription { get; set; }
        public String WebSiteKeywords { get; set; }
        public string Server_url_1 { get; set; }
        public string Server_url_2 { get; set; }
        public string Server_url_3 { get; set; }
        #endregion
    }
}
