namespace BankDds.Infrastructure.Security;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string UserGroup { get; set; } = string.Empty; // "NganHang", "ChiNhanh", "KhachHang"
    public string DefaultBranch { get; set; } = string.Empty;
    public string? CustomerCMND { get; set; }
    public int? EmployeeId { get; set; }
}
