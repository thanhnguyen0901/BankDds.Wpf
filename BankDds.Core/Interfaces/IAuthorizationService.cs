using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines authorization checks for roles, branches, and business actions.
    /// </summary>
    public interface IAuthorizationService
    {
        /// <summary>
        /// Determines whether current session can open administration features.
        /// </summary>
        /// <returns>True when current session is allowed to use administration features; otherwise false.</returns>
        bool CanAccessAdmin();

        /// <summary>
        /// Determines whether current session can create a login in the target role.
        /// </summary>
        /// <param name="targetUserGroup">Target user role.</param>
        /// <returns>True when current session can create a login with the target role; otherwise false.</returns>
        bool CanCreateUser(UserGroup targetUserGroup);

        /// <summary>
        /// Determines whether current session can read data of the selected branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>True when current session can read data of the requested branch; otherwise false.</returns>
        bool CanAccessBranch(string branchCode);

        /// <summary>
        /// Determines whether current session can update data of the selected branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>True when current session can modify data of the requested branch; otherwise false.</returns>
        bool CanModifyBranch(string branchCode);

        /// <summary>
        /// Determines whether current session can view the target customer profile.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>True when current session can read the requested customer; otherwise false.</returns>
        bool CanAccessCustomer(string cmnd);

        /// <summary>
        /// Determines whether current session can view accounts owned by the target customer.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        /// <returns>True when current session can read accounts of the requested customer; otherwise false.</returns>
        bool CanAccessAccount(string cmnd);

        /// <summary>
        /// Determines whether current session can post transactions in the selected branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>True when current session can execute branch transactions; otherwise false.</returns>
        bool CanPerformTransactions(string branchCode);

        /// <summary>
        /// Determines whether current session can open reports for the selected scope.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>True when current session can open reports for the selected scope; otherwise false.</returns>
        bool CanAccessReports(string? branchCode = null);

        /// <summary>
        /// Ensures user has administration permission; otherwise throws.
        /// </summary>
        void RequireAdminAccess();

        /// <summary>
        /// Ensures user can create login for the target role; otherwise throws.
        /// </summary>
        /// <param name="targetUserGroup">Target user role.</param>
        void RequireCanCreateUser(UserGroup targetUserGroup);

        /// <summary>
        /// Ensures user can access branch data; otherwise throws.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        void RequireCanAccessBranch(string branchCode);

        /// <summary>
        /// Ensures user can modify branch data; otherwise throws.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        void RequireCanModifyBranch(string branchCode);

        /// <summary>
        /// Ensures user can access customer data; otherwise throws.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        void RequireCanAccessCustomer(string cmnd);

        /// <summary>
        /// Ensures user can access account data; otherwise throws.
        /// </summary>
        /// <param name="cmnd">Customer national ID.</param>
        void RequireCanAccessAccount(string cmnd);

        /// <summary>
        /// Determines whether current session can manage users in the target branch.
        /// </summary>
        /// <param name="userDefaultBranch">Default branch of target user.</param>
        /// <returns>True when current session can manage users in the target branch; otherwise false.</returns>
        bool CanManageUserInBranch(string userDefaultBranch);

        /// <summary>
        /// Ensures user can manage login in target branch; otherwise throws.
        /// </summary>
        /// <param name="userDefaultBranch">Default branch of target user.</param>
        void RequireCanManageUserInBranch(string userDefaultBranch);

        /// <summary>
        /// Ensures user can perform branch transactions; otherwise throws.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        void RequireCanPerformTransactions(string branchCode);

        /// <summary>
        /// Ensures user can access report scope; otherwise throws.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        void RequireCanAccessReports(string? branchCode = null);

        /// <summary>
        /// Gets effective branch filter applied to data queries.
        /// </summary>
        /// <returns>The effective branch code filter, or null when scope is global.</returns>
        string? GetEffectiveBranchFilter();

    }
}
