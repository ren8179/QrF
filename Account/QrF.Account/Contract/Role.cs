using QrF.Framework.Contract;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace QrF.Account.Contract
{
    [Auditable]
    [Table("Role")]
    public class Role : ModelBase
    {
        public string Name { get; set; }
        public string Info { get; set; }

        public virtual List<User> Users { get; set; }
        public string BusinessPermissionString { get; set; }
        [NotMapped]
        public List<int> BusinessPermissionList
        {
            get
            {
                if (string.IsNullOrEmpty(BusinessPermissionString))
                    return new List<int>();
                else
                    return BusinessPermissionString.Split(",".ToCharArray()).Select(p => int.Parse(p)).ToList();
            }
            set
            {
                BusinessPermissionString = string.Join(",", value.Select(p => (int)p));
            }
        }
    }
}
