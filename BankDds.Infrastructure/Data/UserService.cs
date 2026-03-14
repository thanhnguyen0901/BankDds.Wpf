using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Linq;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// User service that delegates to IUserRepository for data access.
/// Enforces authorization rules for user management.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserSession _userSession;

    public UserService(
        IUserRepository userRepository,
        IAuthorizationService authorizationService,
        IUserSession userSession)
    {
        _userRepository = userRepository;
        _authorizationService = authorizationService;
        _userSession = userSession;
    }

    public Task<User?> GetUserAsync(string username)
    {
        // Admin access required to view users
        _authorizationService.RequireAdminAccess();
        return _userRepository.GetUserAsync(username);
    }

    public Task<bool> AddUserAsync(User user)
    {
        // Check if user can access admin
        _authorizationService.RequireAdminAccess();

        var preparedUser = PrepareUserForCreate(user);

        // Check if user can create this type of user
        _authorizationService.RequireCanCreateUser(preparedUser.UserGroup);

        // ChiNhanh admins may only create logins for their own branch.
        _authorizationService.RequireCanManageUserInBranch(preparedUser.DefaultBranch);

        return _userRepository.AddUserAsync(preparedUser);
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        // Admin access required
        _authorizationService.RequireAdminAccess();

        // In SQL-login mode, password reset in admin flow is NGANHANG-only.
        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            throw new UnauthorizedAccessException(
                "Only Bank administrators can reset other users' passwords.");
        }

        // Password reset path in this module is NGANHANG-only and does not
        // change business mapping/scope; repository only calls sp_DoiMatKhau.
        return _userRepository.UpdateUserAsync(user);
    }

    public Task<bool> DeleteUserAsync(string username)
    {
        // Admin access required
        _authorizationService.RequireAdminAccess();

        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            throw new UnauthorizedAccessException(
                "Only Bank administrators can delete SQL logins.");
        }
        
        return _userRepository.DeleteUserAsync(username);
    }

    public Task<bool> RestoreUserAsync(string username)
    {
        // Legacy operation retained for compatibility.
        _authorizationService.RequireAdminAccess();
        return _userRepository.RestoreUserAsync(username);
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        // Admin access required to list all users
        _authorizationService.RequireAdminAccess();

        var users = await _userRepository.GetAllUsersAsync();

        if (_userSession.UserGroup == UserGroup.NganHang)
            return users;

        // ChiNhanh: only users in own branch and only manageable groups.
        var branch = _userSession.SelectedBranch.Trim().ToUpperInvariant();
        return users
            .Where(u => u.DefaultBranch.Trim().ToUpperInvariant() == branch)
            .Where(u => _authorizationService.CanCreateUser(u.UserGroup))
            .ToList();
    }

    private User PrepareUserForCreate(User input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));

        var prepared = new User
        {
            Username = input.Username.Trim(),
            PasswordHash = input.PasswordHash,
            UserGroup = input.UserGroup,
            DefaultBranch = (input.DefaultBranch ?? string.Empty).Trim().ToUpperInvariant(),
            CustomerCMND = string.IsNullOrWhiteSpace(input.CustomerCMND) ? null : input.CustomerCMND.Trim(),
            EmployeeId = string.IsNullOrWhiteSpace(input.EmployeeId) ? null : input.EmployeeId.Trim().ToUpperInvariant(),
            TrangThaiXoa = 0
        };

        if (_userSession.UserGroup == UserGroup.ChiNhanh)
        {
            prepared.DefaultBranch = _userSession.SelectedBranch;
        }

        if (prepared.UserGroup == UserGroup.KhachHang)
        {
            prepared.EmployeeId = null;
        }
        else if (prepared.UserGroup == UserGroup.ChiNhanh)
        {
            prepared.CustomerCMND = null;
        }
        else
        {
            prepared.CustomerCMND = null;
            prepared.EmployeeId = null;
        }

        return prepared;
    }
}
