using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    public interface IAuthorizationService
    {
        bool CanAccessAdmin();
        bool CanCreateUser(UserGroup targetUserGroup);
        bool CanAccessBranch(string branchCode);
        bool CanModifyBranch(string branchCode);
        bool CanAccessCustomer(string cmnd);
        bool CanAccessAccount(string cmnd);
        bool CanPerformTransactions(string branchCode);
        bool CanAccessReports(string? branchCode = null);
        void RequireAdminAccess();
        void RequireCanCreateUser(UserGroup targetUserGroup);
        void RequireCanAccessBranch(string branchCode);
        void RequireCanModifyBranch(string branchCode);
        void RequireCanAccessCustomer(string cmnd);
        void RequireCanAccessAccount(string cmnd);
        bool CanManageUserInBranch(string userDefaultBranch);
        void RequireCanManageUserInBranch(string userDefaultBranch);
        void RequireCanPerformTransactions(string branchCode);
        void RequireCanAccessReports(string? branchCode = null);
        string? GetEffectiveBranchFilter();
    }
}