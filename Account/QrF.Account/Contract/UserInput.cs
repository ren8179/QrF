using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QrF.Account.Contract
{
    public class UserInput
    {
        public int ID { get; set; }
        public string LoginName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string NewPassword { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
    }
}
