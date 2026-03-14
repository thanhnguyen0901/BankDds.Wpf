namespace BankDds.Core.Models
{
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