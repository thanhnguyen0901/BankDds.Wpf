namespace BankDds.Core.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserGroup UserGroup { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public string? CustomerCMND { get; set; }
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Legacy soft-delete flag from NGUOIDUNG transitional model.
    /// In SQL-login mode, account deletion is hard delete (drop login/user),
    /// so this flag is typically 0 for listed accounts.
    /// </summary>
    public int TrangThaiXoa { get; set; } = 0;

    /// <summary>Display-friendly status label for DataGrid binding.</summary>
    public string StatusText => TrangThaiXoa == 0 ? "Hoạt động" : "Đã xóa";
}
