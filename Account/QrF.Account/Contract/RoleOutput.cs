using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QrF.Account.Contract
{
    public class RoleOutput
    {
        public int ID { get; set; }
        public string Name { get; set; }
        
        public string Info { get; set; }
        public string BusinessPermissionString { get; set; }
    }
}
