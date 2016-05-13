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
        public string ApkHost { get; set; }
        public int DayTaskCount { get; set; }
        public int CashingPhoneBill { get; set; }
        public string Host_2 { get; set; }
        public string ImgHost { get; set; }
        public string Letou2_dx_Host { get; set; }
        public string Letou2_lt_Host { get; set; }
        public string Letou2_Port { get; set; }
        public string MobileAPI { get; set; }
        public string Mobile_dx_Host { get; set; }
        public string Mobile_lt_Host { get; set; }
        public string Mobile_Port { get; set; }
        public string Port_2 { get; set; }
        public string Test_Host { get; set; }
        public string Test_Port { get; set; }
        public string WebSiteDescription { get; set; }
        public string WebSiteKeywords { get; set; }
        public string WebSiteTitle { get; set; }
        #endregion
    }
}
