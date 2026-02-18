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
    /// Soft-delete flag. 0 = Active, 1 = Deleted.
    /// Deleted users cannot log in and are hidden from active lists but retained in the database.
    /// SQL: corresponds to TRANGTHAIXED tinyint column with CHECK (TRANGTHAIXED IN (0,1)).
    /// SP contract: SP_SoftDeleteUser @Username → SET TRANGTHAIXED=1; SP_RestoreUser @Username → SET TRANGTHAIXED=0.
    /// </summary>
    public int TrangThaiXoa { get; set; } = 0;

    /// <summary>Display-friendly status label for DataGrid binding.</summary>
    public string StatusText => TrangThaiXoa == 0 ? "Hoạt động" : "Đã xóa";
}
