using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace QrF.Account.Contract
{
    [Serializable]
    public class AdminMenuConfig
    {
        public AdminMenuGroup[] AdminMenuGroups { get; set; }
    }

    [Serializable]
    public class AdminMenuGroup
    {
        public List<AdminMenu> AdminMenuArray { get; set; }
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("url")]
        public string Url { get; set; }

        [XmlAttribute("icon")]
        public string Icon { get; set; }

        [XmlAttribute("code")]
        public string Code { get; set; }

        [XmlAttribute("permission")]
        public string Permission { get; set; }

        [XmlAttribute("info")]
        public string Info { get; set; }
    }

    [Serializable]
    public class AdminMenu
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("url")]
        public string Url { get; set; }
        public string Icon { get; set; }

        [XmlAttribute("info")]
        public string Info { get; set; }

        [XmlAttribute("code")]
        public string Code { get; set; }

        [XmlAttribute("permission")]
        public string Permission { get; set; }
    }
}
