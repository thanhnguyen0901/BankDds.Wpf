namespace BankDds.Infrastructure.Security;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string userName, string password);
    Task LogoutAsync();
}
