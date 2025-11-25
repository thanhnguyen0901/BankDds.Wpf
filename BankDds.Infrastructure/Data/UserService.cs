using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class UserService : IUserService
{
    private readonly List<User> _users = new()
    {
        new User { Username = "admin", Password = "123", UserGroup = UserGroup.NganHang, DefaultBranch = "ALL" },
        new User { Username = "btuser", Password = "123", UserGroup = UserGroup.ChiNhanh, DefaultBranch = "BENTHANH" },
        new User { Username = "tduser", Password = "123", UserGroup = UserGroup.ChiNhanh, DefaultBranch = "TANDINH" },
        new User { Username = "c123456", Password = "123", UserGroup = UserGroup.KhachHang, DefaultBranch = "BENTHANH", CustomerCMND = "c123456" }
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

        existing.Password = user.Password;
        existing.UserGroup = user.UserGroup;
        existing.DefaultBranch = user.DefaultBranch;
        existing.CustomerCMND = user.CustomerCMND;

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
