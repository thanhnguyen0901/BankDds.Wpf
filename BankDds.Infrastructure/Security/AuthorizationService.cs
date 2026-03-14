using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Security
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IUserSession _userSession;

        public AuthorizationService(IUserSession userSession)
        {
            _userSession = userSession;
        }

        public bool CanAccessAdmin()
        {
            return _userSession.UserGroup == UserGroup.NganHang ||
                   _userSession.UserGroup == UserGroup.ChiNhanh;
        }

        public bool CanCreateUser(UserGroup targetUserGroup)
        {
            if (_userSession.UserGroup == UserGroup.NganHang)
                return targetUserGroup == UserGroup.NganHang;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return targetUserGroup is UserGroup.ChiNhanh or UserGroup.KhachHang;
            return false;
        }

        public bool CanAccessBranch(string branchCode)
        {
            if (_userSession.UserGroup == UserGroup.NganHang)
                return true;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return branchCode == _userSession.SelectedBranch;
            if (_userSession.UserGroup == UserGroup.KhachHang)
                return branchCode == _userSession.SelectedBranch;
            return false;
        }

        public bool CanModifyBranch(string branchCode)
        {
            if (_userSession.UserGroup == UserGroup.NganHang)
                return false;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return branchCode == _userSession.SelectedBranch;
            return false;
        }

        public bool CanAccessCustomer(string cmnd)
        {
            if (_userSession.UserGroup == UserGroup.NganHang)
                return true;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return true;
            if (_userSession.UserGroup == UserGroup.KhachHang)
                return cmnd == _userSession.CustomerCMND;
            return false;
        }

        public bool CanAccessAccount(string cmnd)
        {
            return CanAccessCustomer(cmnd);
        }

        public bool CanPerformTransactions(string branchCode)
        {
            if (_userSession.UserGroup == UserGroup.KhachHang)
                return false;
            return CanModifyBranch(branchCode);
        }

        public bool CanAccessReports(string? branchCode = null)
        {
            if (_userSession.UserGroup == UserGroup.NganHang)
                return true;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return branchCode == null || branchCode == _userSession.SelectedBranch;
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
            if (_userSession.UserGroup == UserGroup.NganHang)
                return true;
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
                return userDefaultBranch == _userSession.SelectedBranch;
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
            if (_userSession.UserGroup == UserGroup.NganHang)
                return null;
            return _userSession.SelectedBranch;
        }
    }
}