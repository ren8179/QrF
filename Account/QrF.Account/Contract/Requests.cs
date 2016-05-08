using QrF.Framework.Contract;

namespace QrF.Account.Contract
{
    public class UserRequest : Request
    {
        public string LoginName { get; set; }
        public string Mobile { get; set; }
        public string IpAddress { get; set; }
        public int? DepartMentId { get; set; }
        public Role Role { get; set; }
    }

    public class RoleRequest : Request
    {
        public string RoleName { get; set; }
    }
    public class MenuRequest : Request
    {
        public string Name { get; set; }
        public int? ParentId { get; set; }
    }
    public class AuditLogRequest : Request
    {
        public int? pageNumber { get; set; }
        public int? pageSize { get; set; }

        public string Name { get; set; }
    }
}
