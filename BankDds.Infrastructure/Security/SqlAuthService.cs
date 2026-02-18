using BankDds.Core.Interfaces;

namespace BankDds.Infrastructure.Security;

/// <summary>
/// Legacy SQL-targeted auth service — NOT registered in the DI container.
/// <para>
/// <see cref="AuthService"/> is the single registered <see cref="IAuthService"/> implementation.
/// It already delegates to <see cref="IUserRepository"/>, which is bound to either
/// <c>InMemoryUserRepository</c> or <c>SqlUserRepository</c> depending on the
/// <c>DataMode</c> setting in <c>appsettings.json</c>.  There is therefore no reason
/// to register this class.  It is kept for reference only and may be deleted.
/// </para>
/// </summary>
[Obsolete("Use AuthService (the registered IAuthService). It handles both InMemory and SQL modes via IUserRepository.")]
public class SqlAuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public SqlAuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthResult> LoginAsync(string userName, string password)
    {
        var user = await _userRepository.GetUserAsync(userName);
        if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return new AuthResult
            {
                Success = true,
                UserGroup = user.UserGroup.ToString(),
                DefaultBranch = user.DefaultBranch,
                CustomerCMND = user.CustomerCMND,
                EmployeeId = user.EmployeeId
            };
        }

        return new AuthResult
        {
            Success = false,
            ErrorMessage = "Invalid username or password"
        };
    }

    public Task LogoutAsync() => Task.CompletedTask;
}
