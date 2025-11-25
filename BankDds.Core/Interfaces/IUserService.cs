using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetUserAsync(string username);
    Task<bool> AddUserAsync(User user);
    Task<bool> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string username);
    Task<List<User>> GetAllUsersAsync();
}
