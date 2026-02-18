namespace BankDds.Infrastructure.Security;

public class SqlAuthService : IAuthService
{
    // Hard-coded users with hashed passwords and employee IDs
    private readonly Dictionary<string, (string PasswordHash, string UserGroup, string DefaultBranch, string? CustomerCMND, int? EmployeeId)> _users = new()
    {
        ["admin"] = (BCrypt.Net.BCrypt.HashPassword("123"), "NganHang", "ALL", null, 1),
        ["btuser"] = (BCrypt.Net.BCrypt.HashPassword("123"), "ChiNhanh", "BENTHANH", null, 2),
        ["tduser"] = (BCrypt.Net.BCrypt.HashPassword("123"), "ChiNhanh", "TANDINH", null, 3),
        ["c123456"] = (BCrypt.Net.BCrypt.HashPassword("123"), "KhachHang", "BENTHANH", "c123456", null)
    };

    public Task<AuthResult> LoginAsync(string serverName, string userName, string password)
    {
        // Verify user exists and password matches
        if (_users.TryGetValue(userName, out var user))
        {
            // Verify password hash
            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return Task.FromResult(new AuthResult
                {
                    Success = true,
                    UserGroup = user.UserGroup,
                    DefaultBranch = user.DefaultBranch,
                    CustomerCMND = user.CustomerCMND,
                    EmployeeId = user.EmployeeId
                });
            }
        }

        // Authentication failed
        return Task.FromResult(new AuthResult
        {
            Success = false,
            ErrorMessage = "Invalid username or password"
        });
    }

    public Task LogoutAsync()
    {
        return Task.CompletedTask;
    }
}
