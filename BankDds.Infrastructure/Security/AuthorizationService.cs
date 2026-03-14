using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Security;

/// <summary>
/// Authorization service implementing role and branch-based access control
/// </summary>
public class AuthorizationService : IAuthorizationService
{
    private readonly IUserSession _userSession;

    public AuthorizationService(IUserSession userSession)
    {
        _userSession = userSession;
    }

    public bool CanAccessAdmin()
    {
        // Both NganHang and ChiNhanh can access admin (but with different privileges)
        return _userSession.UserGroup == UserGroup.NganHang || 
               _userSession.UserGroup == UserGroup.ChiNhanh;
    }

    public bool CanCreateUser(UserGroup targetUserGroup)
    {
        // NganHang can only create NganHang logins.
        if (_userSession.UserGroup == UserGroup.NganHang)
            return targetUserGroup == UserGroup.NganHang;

        // ChiNhanh can create ChiNhanh and KhachHang logins
        // (branch-level onboarding extension in DE3 authz workflow).
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return targetUserGroup is UserGroup.ChiNhanh or UserGroup.KhachHang;

        // KhachHang cannot create users
        return false;
    }

    public bool CanAccessBranch(string branchCode)
    {
        // NganHang can access any branch
        if (_userSession.UserGroup == UserGroup.NganHang)
            return true;

        // ChiNhanh can only access their own branch
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return branchCode == _userSession.SelectedBranch;

        // KhachHang can only access their assigned branch (for viewing own accounts)
        if (_userSession.UserGroup == UserGroup.KhachHang)
            return branchCode == _userSession.SelectedBranch;

        return false;
    }

    public bool CanModifyBranch(string branchCode)
    {
        // Banking rule: NganHang is view-only - can choose branch to view data/reports
        // but CANNOT perform CRUD operations.
        if (_userSession.UserGroup == UserGroup.NganHang)
            return false;

        // ChiNhanh can only modify their own branch (full CRUD)
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return branchCode == _userSession.SelectedBranch;

        // KhachHang cannot modify any branch data
        return false;
    }

    public bool CanAccessCustomer(string cmnd)
    {
        // NganHang can access any customer in selected branch context
        if (_userSession.UserGroup == UserGroup.NganHang)
            return true;

        // ChiNhanh can access customers in their branch
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return true; // Branch validation will be done at repository level

        // KhachHang can only access their own CMND
        if (_userSession.UserGroup == UserGroup.KhachHang)
            return cmnd == _userSession.CustomerCMND;

        return false;
    }

    public bool CanAccessAccount(string cmnd)
    {
        // Same logic as customer access
        return CanAccessCustomer(cmnd);
    }

    public bool CanPerformTransactions(string branchCode)
    {
        // Only NganHang and ChiNhanh employees can perform transactions
        if (_userSession.UserGroup == UserGroup.KhachHang)
            return false;

        // Must be able to modify the branch
        return CanModifyBranch(branchCode);
    }

    public bool CanAccessReports(string? branchCode = null)
    {
        // NganHang can access reports for any branch
        if (_userSession.UserGroup == UserGroup.NganHang)
            return true;

        // ChiNhanh can access reports for their own branch only.
        // Passing branchCode="ALL" is a cross-branch read - ChiNhanh must not see it.
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return branchCode == null || branchCode == _userSession.SelectedBranch;

        // KhachHang can only access their own account statement (not general reports)
        return false;
    }

    public void RequireAdminAccess()
    {
        if (!CanAccessAdmin())
        {
            throw new UnauthorizedAccessException(
                $"Nhóm người dùng '{_userSession.UserGroup}' không có quyền truy cập quản trị người dùng.");
        }
    }

    public void RequireCanCreateUser(UserGroup targetUserGroup)
    {
        if (!CanCreateUser(targetUserGroup))
        {
            throw new UnauthorizedAccessException(
                $"Nhóm người dùng '{_userSession.UserGroup}' không có quyền tạo tài khoản loại '{targetUserGroup}'.");
        }
    }

    public void RequireCanAccessBranch(string branchCode)
    {
        if (!CanAccessBranch(branchCode))
        {
            throw new UnauthorizedAccessException(
                $"Bạn không có quyền truy cập chi nhánh '{branchCode}'.");
        }
    }

    public void RequireCanModifyBranch(string branchCode)
    {
        if (!CanModifyBranch(branchCode))
        {
            throw new UnauthorizedAccessException(
                $"Bạn không có quyền cập nhật dữ liệu tại chi nhánh '{branchCode}'.");
        }
    }

    public void RequireCanAccessCustomer(string cmnd)
    {
        if (!CanAccessCustomer(cmnd))
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền truy cập dữ liệu khách hàng này.");
        }
    }

    public void RequireCanAccessAccount(string cmnd)
    {
        if (!CanAccessAccount(cmnd))
        {
            throw new UnauthorizedAccessException(
                "Bạn không có quyền truy cập tài khoản này.");
        }
    }

    public bool CanManageUserInBranch(string userDefaultBranch)
    {
        // NganHang can manage users in any branch
        if (_userSession.UserGroup == UserGroup.NganHang)
            return true;

        // ChiNhanh can only manage users whose DefaultBranch matches their own
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
            return userDefaultBranch == _userSession.SelectedBranch;

        // KhachHang cannot manage any login accounts
        return false;
    }

    public void RequireCanManageUserInBranch(string userDefaultBranch)
    {
        if (!CanManageUserInBranch(userDefaultBranch))
        {
            throw new UnauthorizedAccessException(
                $"Bạn không có quyền quản lý đăng nhập cho chi nhánh '{userDefaultBranch}'.");
        }
    }

    public void RequireCanPerformTransactions(string branchCode)
    {
        if (!CanPerformTransactions(branchCode))
        {
            throw new UnauthorizedAccessException(
                $"Bạn không có quyền thực hiện giao dịch tại chi nhánh '{branchCode}'.");
        }
    }

    public void RequireCanAccessReports(string? branchCode = null)
    {
        if (!CanAccessReports(branchCode))
        {
            string scope = branchCode == null ? "tất cả chi nhánh" : $"chi nhánh '{branchCode}'";
            throw new UnauthorizedAccessException(
                $"Bạn không có quyền truy cập báo cáo cho phạm vi {scope}.");
        }
    }

    public string? GetEffectiveBranchFilter()
    {
        // NganHang sees everything - no filter
        if (_userSession.UserGroup == UserGroup.NganHang)
            return null;

        // ChiNhanh and KhachHang are scoped to their assigned branch
        return _userSession.SelectedBranch;
    }
}



