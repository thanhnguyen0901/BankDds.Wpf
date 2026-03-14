namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents an application login mapped to role, branch scope, and person identity.
    /// </summary>
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserGroup UserGroup { get; set; }
        public string DefaultBranch { get; set; } = string.Empty;
        public string? CustomerCMND { get; set; }
        public string? EmployeeId { get; set; }
        public int TrangThaiXoa { get; set; } = 0;
        public string StatusText => TrangThaiXoa == 0 ? "Hoạt động" : "Đã xóa";
    }
}
