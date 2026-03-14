using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Executes user-account administration flow and enforces role-based creation rules.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserSession _userSession;

        /// <summary>
        /// Initializes user service with repository, authorization, and active session context.
        /// </summary>
        /// <param name="userRepository">User data repository.</param>
        /// <param name="authorizationService">Authorization service for role and branch checks.</param>
        /// <param name="userSession">Current authenticated user session.</param>
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
            _authorizationService.RequireAdminAccess();
            return _userRepository.GetUserAsync(username);
        }

        public Task<bool> AddUserAsync(User user)
        {
            _authorizationService.RequireAdminAccess();

            var preparedUser = PrepareUserForCreate(user);

            _authorizationService.RequireCanCreateUser(preparedUser.UserGroup);
            _authorizationService.RequireCanManageUserInBranch(preparedUser.DefaultBranch);
            return _userRepository.AddUserAsync(preparedUser);
        }

        public Task<bool> UpdateUserAsync(User user)
        {
            _authorizationService.RequireAdminAccess();

            // Logic: resetting other user credentials is restricted to NganHang in current policy.
            if (_userSession.UserGroup != UserGroup.NganHang)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ quản trị viên NganHang mới được đặt lại mật khẩu cho tài khoản khác.");
            }

            return _userRepository.UpdateUserAsync(user);
        }

        public Task<bool> DeleteUserAsync(string username)
        {
            _authorizationService.RequireAdminAccess();

            // Logic: dropping SQL login is restricted to NganHang.
            if (_userSession.UserGroup != UserGroup.NganHang)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ quản trị viên NganHang mới được xóa SQL login.");
            }

            return _userRepository.DeleteUserAsync(username);
        }

        public Task<bool> RestoreUserAsync(string username)
        {
            _authorizationService.RequireAdminAccess();
            return _userRepository.RestoreUserAsync(username);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            _authorizationService.RequireAdminAccess();

            var users = await _userRepository.GetAllUsersAsync();

            if (_userSession.UserGroup == UserGroup.NganHang)
            {
                return users;
            }

            // Logic: ChiNhanh can only manage users in its own branch and in roles it can create.
            var branch = _userSession.SelectedBranch.Trim().ToUpperInvariant();

            return users
                .Where(u => u.DefaultBranch.Trim().ToUpperInvariant() == branch)
                .Where(u => _authorizationService.CanCreateUser(u.UserGroup))
                .ToList();
        }

        private User PrepareUserForCreate(User input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

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

            // Logic: ChiNhanh always creates users in current branch regardless of UI input value.
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
            {
                prepared.DefaultBranch = _userSession.SelectedBranch;
            }

            // Logic: keep identity mapping consistent with target role.
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
}
