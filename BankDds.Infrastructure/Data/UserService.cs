using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// User service that delegates to IUserRepository for data access.
/// Enforces authorization rules for user management.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthorizationService _authorizationService;

    public UserService(IUserRepository userRepository, IAuthorizationService authorizationService)
    {
        _userRepository = userRepository;
        _authorizationService = authorizationService;
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

        // Check if user can create this type of user
        _authorizationService.RequireCanCreateUser(user.UserGroup);

        // ChiNhanh admins may only create logins for their own branch
        _authorizationService.RequireCanManageUserInBranch(user.DefaultBranch);

        return _userRepository.AddUserAsync(user);
    }

    public Task<bool> UpdateUserAsync(User user)
    {
        // Admin access required
        _authorizationService.RequireAdminAccess();

        // Check if user can modify this type of user
        _authorizationService.RequireCanCreateUser(user.UserGroup);

        // ChiNhanh admins may only update logins belonging to their own branch
        _authorizationService.RequireCanManageUserInBranch(user.DefaultBranch);

        return _userRepository.UpdateUserAsync(user);
    }

    public Task<bool> DeleteUserAsync(string username)
    {
        // Admin access required
        _authorizationService.RequireAdminAccess();
        
        return _userRepository.DeleteUserAsync(username);
    }

    public Task<bool> RestoreUserAsync(string username)
    {
        // Admin access required to restore soft-deleted users
        _authorizationService.RequireAdminAccess();
        return _userRepository.RestoreUserAsync(username);
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        // Admin access required to list all users
        _authorizationService.RequireAdminAccess();
        return _userRepository.GetAllUsersAsync();
    }
}
