namespace BankDds.Core.Models;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserGroup UserGroup { get; set; }
    public string DefaultBranch { get; set; } = string.Empty;
    public string? CustomerCMND { get; set; }
}
