using QrF.Framework.Contract;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace QrF.Account.Contract
{
    [Serializable]
    [Table("VerifyCode")]
    public class VerifyCode : ModelBase
    {
        public Guid Guid { get; set; }
        public string VerifyText { get; set; }
    }
}
