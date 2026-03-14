using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines authenticated session state and current branch context.
    /// </summary>
    public interface IUserSession
    {
        string Username { get; }
        string DisplayName { get; }
        UserGroup UserGroup { get; }
        string SelectedBranch { get; }
        List<string> PermittedBranches { get; }
        string? CustomerCMND { get; }
        string? EmployeeId { get; }
        /// <summary>
        /// Sets authenticated session context after successful login.
        /// </summary>
        /// <param name="username">Login name.</param>
        /// <param name="displayName">Display name shown in UI.</param>
        /// <param name="userGroup">Current user role.</param>
        /// <param name="selectedBranch">Currently selected branch code.</param>
        /// <param name="permittedBranches">Branches that current user is allowed to access.</param>
        /// <param name="customerCMND">Customer national ID.</param>
        /// <param name="employeeId">Employee code.</param>
        void SetSession(
            string username,
            string displayName,
            UserGroup userGroup,
            string selectedBranch,
            List<string> permittedBranches,
            string? customerCMND = null,
            string? employeeId = null);

        /// <summary>
        /// Clears authenticated session context.
        /// </summary>
        void ClearSession();

        bool IsAuthenticated { get; }
        event Action? SelectedBranchChanged;
        /// <summary>
        /// Changes current working branch in active session.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        void SetSelectedBranch(string branchCode);

    }
}
