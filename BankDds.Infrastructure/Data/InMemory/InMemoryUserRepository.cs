using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory implementation of IUserRepository for development and testing.
/// Passwords are stored as BCrypt hashes.
/// </summary>
public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new()
    {
        // CustomerCMND must be numeric 9–12 digits (matches nChar CMND column).
        new User { Username = "admin",   PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), UserGroup = UserGroup.NganHang,  DefaultBranch = "ALL",      EmployeeId = "NV00000001" },
        new User { Username = "btuser",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), UserGroup = UserGroup.ChiNhanh,  DefaultBranch = "BENTHANH", EmployeeId = "NV00000002" },
        new User { Username = "tduser",  PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), UserGroup = UserGroup.ChiNhanh,  DefaultBranch = "TANDINH",  EmployeeId = "NV00000003" },
        new User { Username = "c123456", PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"), UserGroup = UserGroup.KhachHang, DefaultBranch = "BENTHANH", CustomerCMND = "056789012", EmployeeId = null }
    };

    public Task<User?> GetUserAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(user);
    }

    public Task<bool> AddUserAsync(User user)
    {
        if (_users.Any(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(false);

        _users.Add(user);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        var existing = _users.FirstOrDefault(u => u.Username.Equals(user.Username, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
            return Task.FromResult(false);

        existing.PasswordHash = user.PasswordHash;
        existing.UserGroup = user.UserGroup;
        existing.DefaultBranch = user.DefaultBranch;
        existing.CustomerCMND = user.CustomerCMND;
        existing.EmployeeId = user.EmployeeId;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteUserAsync(string username)
    {
        var user = _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        if (user == null)
            return Task.FromResult(false);

        _users.Remove(user);
        return Task.FromResult(true);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_users.ToList());
    }
}
