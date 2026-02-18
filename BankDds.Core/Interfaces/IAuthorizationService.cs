using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Authorization service for role and branch-based access control
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Check if current user can perform admin operations
    /// </summary>
    bool CanAccessAdmin();

    /// <summary>
    /// Check if current user can create a user of the specified group
    /// </summary>
    bool CanCreateUser(UserGroup targetUserGroup);

    /// <summary>
    /// Check if current user can access data for the specified branch
    /// </summary>
    bool CanAccessBranch(string branchCode);

    /// <summary>
    /// Check if current user can modify data in the specified branch
    /// </summary>
    bool CanModifyBranch(string branchCode);

    /// <summary>
    /// Check if current user can view customer data
    /// </summary>
    bool CanAccessCustomer(string cmnd);

    /// <summary>
    /// Check if current user can view account data
    /// </summary>
    bool CanAccessAccount(string cmnd);

    /// <summary>
    /// Check if current user can perform transactions
    /// </summary>
    bool CanPerformTransactions(string branchCode);

    /// <summary>
    /// Check if current user can access reports for the specified branch
    /// </summary>
    bool CanAccessReports(string? branchCode = null);

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot access admin
    /// </summary>
    void RequireAdminAccess();

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot create specified user group
    /// </summary>
    void RequireCanCreateUser(UserGroup targetUserGroup);

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot access branch
    /// </summary>
    void RequireCanAccessBranch(string branchCode);

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot modify branch
    /// </summary>
    void RequireCanModifyBranch(string branchCode);

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot access customer
    /// </summary>
    void RequireCanAccessCustomer(string cmnd);

    /// <summary>
    /// Throws UnauthorizedAccessException if user cannot access account
    /// </summary>
    void RequireCanAccessAccount(string cmnd);

    /// <summary>
    /// Check if current user can create/manage a user whose DefaultBranch is <paramref name="userDefaultBranch"/>.
    /// NganHang: always true. ChiNhanh: only for their own branch. KhachHang: never.
    /// </summary>
    bool CanManageUserInBranch(string userDefaultBranch);

    /// <summary>
    /// Throws UnauthorizedAccessException if the user cannot manage a login in the given branch.
    /// </summary>
    void RequireCanManageUserInBranch(string userDefaultBranch);

    /// <summary>
    /// Throws UnauthorizedAccessException if current user cannot perform transactions in the given branch.
    /// </summary>
    void RequireCanPerformTransactions(string branchCode);

    /// <summary>
    /// Throws UnauthorizedAccessException if current user cannot access reports for the given branch.
    /// Pass null to check general report access.
    /// </summary>
    void RequireCanAccessReports(string? branchCode = null);

    /// <summary>
    /// Returns the branch code that should be used to filter data for the current user.
    /// Returns null for NganHang (no restriction) and the user's own branch for ChiNhanh / KhachHang.
    /// </summary>
    string? GetEffectiveBranchFilter();
}
