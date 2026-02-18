using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for User data access operations
/// </summary>
public interface IUserRepository
{
    Task<User?> GetUserAsync(string username);
    Task<bool> AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string username);
    /// <summary>Restores a soft-deleted user (sets TrangThaiXoa = 0).</summary>
    Task<bool> RestoreUserAsync(string username);
    Task<List<User>> GetAllUsersAsync();
}
