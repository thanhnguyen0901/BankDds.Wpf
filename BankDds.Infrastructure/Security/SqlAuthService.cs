namespace BankDds.Infrastructure.Security;

public class SqlAuthService : IAuthService
{
    // Hard-coded users
    private readonly Dictionary<string, (string Password, string UserGroup, string DefaultBranch, string? CustomerCMND)> _users = new()
    {
        ["admin"] = ("123", "NganHang", "ALL", null),
        ["btuser"] = ("123", "ChiNhanh", "BENTHANH", null),
        ["tduser"] = ("123", "ChiNhanh", "TANDINH", null),
        ["c123456"] = ("123", "KhachHang", "BENTHANH", "c123456")
    };

    public Task<AuthResult> LoginAsync(string serverName, string userName, string password)
    {
        // TEMPORARY: Bypass authentication for testing - accept any username/password
        // TODO: Remove this bypass before production
        
        // If user exists in dictionary, use their info
        if (_users.TryGetValue(userName, out var user))
        {
            return Task.FromResult(new AuthResult
            {
                Success = true,
                UserGroup = user.UserGroup,
                DefaultBranch = user.DefaultBranch,
                CustomerCMND = user.CustomerCMND
            });
        }

        // For any other username, login as NganHang (Bank Level) for testing
        return Task.FromResult(new AuthResult
        {
            Success = true,
            UserGroup = "NganHang",
            DefaultBranch = "ALL",
            CustomerCMND = null
        });
    }

    public Task LogoutAsync()
    {
        return Task.CompletedTask;
    }
}
