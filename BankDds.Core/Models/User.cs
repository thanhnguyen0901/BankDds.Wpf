namespace BankDds.Core.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserGroup UserGroup { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public string? CustomerCMND { get; set; }
    public int? EmployeeId { get; set; }
}
