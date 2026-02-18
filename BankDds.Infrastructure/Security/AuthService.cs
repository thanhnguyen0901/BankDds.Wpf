using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Security;

/// <summary>
/// Unified authentication service using IUserRepository as single source of truth
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<AuthResult> LoginAsync(string userName, string password)
    {
        try
        {
            // Get user from repository
            var user = await _userRepository.GetUserAsync(userName);

            if (user == null)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password"
                };
            }

            // Verify password hash using BCrypt
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Invalid username or password"
                };
            }

            // Block soft-deleted accounts from logging in
            if (user.TrangThaiXoa == 1)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Tài khoản này đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên."
                };
            }

            // Convert UserGroup enum to string for compatibility
            string userGroupString = user.UserGroup switch
            {
                UserGroup.NganHang => "NganHang",
                UserGroup.ChiNhanh => "ChiNhanh",
                UserGroup.KhachHang => "KhachHang",
                _ => "Unknown"
            };

            return new AuthResult
            {
                Success = true,
                UserGroup = userGroupString,
                DefaultBranch = user.DefaultBranch,
                CustomerCMND = user.CustomerCMND,
                EmployeeId = user.EmployeeId
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Login error: {ex.Message}"
            };
        }
    }

    public Task LogoutAsync()
    {
        // No cleanup needed for repository-based auth
        return Task.CompletedTask;
    }
}
