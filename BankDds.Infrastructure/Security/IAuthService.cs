namespace BankDds.Infrastructure.Security;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string serverName, string userName, string password);
    Task LogoutAsync();
}
