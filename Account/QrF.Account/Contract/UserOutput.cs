using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QrF.Account.Contract
{
    public class UserOutput
    {
        public int ID { get; set; }
        public string LoginName { get; set; }
        public string UserName { get; set; }
        public string AppId { get; set; }
        public string PartnerId { get; set; }
        public string IpAddress { get; set; }
        public string Roles { get; set; }
        public string RoleIds { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public bool IsActive { get; set; }
        public string Password { get; set; }
    }
}
