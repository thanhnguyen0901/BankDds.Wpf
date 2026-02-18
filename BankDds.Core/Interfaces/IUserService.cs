using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetUserAsync(string username);
    Task<bool> AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string username);
    /// <summary>Restores a soft-deleted user (sets TrangThaiXoa = 0).</summary>
    Task<bool> RestoreUserAsync(string username);
    Task<List<User>> GetAllUsersAsync();
}
