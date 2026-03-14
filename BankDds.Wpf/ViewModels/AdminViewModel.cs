using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using Caliburn.Micro;
using System.Collections.ObjectModel;
using System.Linq;

namespace BankDds.Wpf.ViewModels;

public class AdminViewModel : BaseViewModel
{
    private readonly IUserService _userService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;
    private readonly UserValidator _validator;
    private readonly IBranchService _branchService;

    private ObservableCollection<User> _users = new();
    private User? _selectedUser;
    private User _editingUser = new();
    private bool _isEditing;
    private UserGroup _selectedEditingUserGroup;
    private string _newPassword = string.Empty;
    private string _confirmPassword = string.Empty;
    private string _passwordValidationMessage = string.Empty;

    public AdminViewModel(
        IUserService userService,
        IUserSession userSession,
        IDialogService dialogService,
        UserValidator validator,
        IBranchService branchService)
    {
        _userService = userService;
        _userSession = userSession;
        _dialogService = dialogService;
        _validator = validator;
        _branchService = branchService;
        DisplayName = "Quản trị người dùng";
    }

    public ObservableCollection<User> Users
    {
        get => _users;
        set
        {
            _users = value;
            NotifyOfPropertyChange(() => Users);
        }
    }

    public User? SelectedUser
    {
        get => _selectedUser;
        set
        {
            _selectedUser = value;
            NotifyOfPropertyChange(() => SelectedUser);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => CanChangeTargetUserGroup);
        }
    }

    public User EditingUser
    {
        get => _editingUser;
        set
        {
            _editingUser = value;
            _selectedEditingUserGroup = _editingUser.UserGroup;
            NotifyOfPropertyChange(() => EditingUser);
            NotifyOfPropertyChange(() => SelectedEditingUserGroup);
            NotifyOfPropertyChange(() => CanEditDefaultBranch);
            NotifyOfPropertyChange(() => ShowEmployeeIdField);
            NotifyOfPropertyChange(() => ShowCustomerCmndField);
            NotifyOfPropertyChange(() => CanSave);
        }
    }

    public UserGroup SelectedEditingUserGroup
    {
        get => _selectedEditingUserGroup;
        set
        {
            if (_selectedEditingUserGroup == value) return;

            _selectedEditingUserGroup = value;
            _editingUser.UserGroup = value;
            ApplyRoleSpecificDefaults();

            NotifyOfPropertyChange(() => SelectedEditingUserGroup);
            NotifyOfPropertyChange(() => EditingUser);
            NotifyOfPropertyChange(() => CanEditDefaultBranch);
            NotifyOfPropertyChange(() => ShowEmployeeIdField);
            NotifyOfPropertyChange(() => ShowCustomerCmndField);
            NotifyOfPropertyChange(() => CanSave);
        }
    }

    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            _isEditing = value;
            NotifyOfPropertyChange(() => IsEditing);
            NotifyOfPropertyChange(() => CanAdd);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => CanSave);
            NotifyOfPropertyChange(() => CanCancel);
            NotifyOfPropertyChange(() => CanChangeTargetUserGroup);
        }
    }

    public string NewPassword
    {
        get => _newPassword;
        set
        {
            _newPassword = value;
            NotifyOfPropertyChange(() => NewPassword);
            NotifyOfPropertyChange(() => CanSave);
            ValidatePassword();
        }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            _confirmPassword = value;
            NotifyOfPropertyChange(() => ConfirmPassword);
            NotifyOfPropertyChange(() => CanSave);
            ValidatePassword();
        }
    }

    public string PasswordValidationMessage
    {
        get => _passwordValidationMessage;
        set
        {
            _passwordValidationMessage = value;
            NotifyOfPropertyChange(() => PasswordValidationMessage);
            NotifyOfPropertyChange(() => IsPasswordValid);
        }
    }

    public bool IsPasswordValid => PasswordValidationMessage.StartsWith("✓");

    public ObservableCollection<UserGroup> AvailableUserGroups { get; } = new();

    // Real branch codes loaded from IBranchService on activate.
    public ObservableCollection<string> AvailableBranches { get; } = new();

    public bool CanEditDefaultBranch => _userSession.UserGroup == UserGroup.NganHang;
    public bool ShowEmployeeIdField => SelectedEditingUserGroup == UserGroup.ChiNhanh;
    public bool ShowCustomerCmndField => SelectedEditingUserGroup == UserGroup.KhachHang;
    public bool CanChangeTargetUserGroup => SelectedUser == null;

    // CanExecute properties
    public bool CanAdd => !IsEditing;
    public bool CanEdit => SelectedUser != null && !IsEditing && _userSession.UserGroup == UserGroup.NganHang;
    public bool CanDelete => SelectedUser != null && !IsEditing && _userSession.UserGroup == UserGroup.NganHang;
    public bool CanRestore => false;
    public bool CanSave => IsEditing &&
                           !string.IsNullOrWhiteSpace(EditingUser.Username) &&
                           !string.IsNullOrWhiteSpace(NewPassword) &&
                           NewPassword == ConfirmPassword &&
                           IsPasswordValid;
    public bool CanCancel => IsEditing;

    private void ValidatePassword()
    {
        if (string.IsNullOrEmpty(NewPassword))
        {
            PasswordValidationMessage = "Mật khẩu là bắt buộc.";
            return;
        }

        if (NewPassword.Length < 8)
        {
            PasswordValidationMessage = "Mật khẩu phải có ít nhất 8 ký tự.";
            return;
        }

        if (!NewPassword.Any(char.IsUpper))
        {
            PasswordValidationMessage = "Mật khẩu phải có ít nhất 1 chữ in hoa.";
            return;
        }

        if (!NewPassword.Any(char.IsLower))
        {
            PasswordValidationMessage = "Mật khẩu phải có ít nhất 1 chữ thường.";
            return;
        }

        if (!NewPassword.Any(char.IsDigit))
        {
            PasswordValidationMessage = "Mật khẩu phải có ít nhất 1 chữ số.";
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            PasswordValidationMessage = "Mật khẩu xác nhận không khớp.";
            return;
        }

        PasswordValidationMessage = "✓ Mật khẩu hợp lệ.";
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);

        if (_userSession.UserGroup != UserGroup.NganHang && _userSession.UserGroup != UserGroup.ChiNhanh)
        {
            ErrorMessage = "Không có quyền: chỉ người dùng Ngân hàng hoặc Chi nhánh được mở màn hình này.";
            return;
        }

        AvailableUserGroups.Clear();
        if (_userSession.UserGroup == UserGroup.NganHang)
        {
            AvailableUserGroups.Add(UserGroup.NganHang);
        }
        else
        {
            AvailableUserGroups.Add(UserGroup.ChiNhanh);
            AvailableUserGroups.Add(UserGroup.KhachHang);
        }
        NotifyOfPropertyChange(() => AvailableUserGroups);

        try
        {
            var branches = await _branchService.GetAllBranchesAsync();
            AvailableBranches.Clear();
            foreach (var b in branches)
            {
                AvailableBranches.Add(b.MACN.Trim().ToUpperInvariant());
            }
        }
        catch
        {
            if (AvailableBranches.Count == 0)
            {
                AvailableBranches.Add("BENTHANH");
                AvailableBranches.Add("TANDINH");
            }
        }

        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            var users = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(users);
        });
    }

    public void Add()
    {
        if (!AvailableUserGroups.Any())
        {
            ErrorMessage = "Không có nhóm người dùng hợp lệ cho tài khoản hiện tại.";
            return;
        }

        EditingUser = new User
        {
            UserGroup = AvailableUserGroups[0],
            DefaultBranch = _userSession.SelectedBranch == "ALL"
                ? (AvailableBranches.FirstOrDefault() ?? "BENTHANH")
                : _userSession.SelectedBranch
        };
        ApplyRoleSpecificDefaults();

        IsEditing = true;
        SelectedUser = null;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordValidationMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedUser == null) return;

        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            ErrorMessage = "Chỉ người dùng Ngân hàng mới được đặt lại mật khẩu cho tài khoản khác.";
            return;
        }

        EditingUser = new User
        {
            Username = SelectedUser.Username,
            PasswordHash = SelectedUser.PasswordHash,
            UserGroup = SelectedUser.UserGroup,
            DefaultBranch = SelectedUser.DefaultBranch,
            CustomerCMND = SelectedUser.CustomerCMND,
            EmployeeId = SelectedUser.EmployeeId,
            TrangThaiXoa = SelectedUser.TrangThaiXoa
        };
        ApplyRoleSpecificDefaults();

        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordValidationMessage = "Nhập mật khẩu mới để đặt lại.";
    }

    public async Task Save()
    {
        if (_userSession.UserGroup == UserGroup.NganHang &&
            EditingUser.UserGroup != UserGroup.NganHang)
        {
            ErrorMessage = "Tài khoản Ngân hàng chỉ được tạo tài khoản cùng nhóm Ngân hàng.";
            return;
        }

        if (_userSession.UserGroup == UserGroup.ChiNhanh &&
            EditingUser.UserGroup == UserGroup.NganHang)
        {
            ErrorMessage = "Tài khoản Chi nhánh không được tạo tài khoản nhóm Ngân hàng.";
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            EditingUser.PasswordHash = NewPassword;
            ApplyRoleSpecificDefaults();

            bool result;

            if (SelectedUser == null)
            {
                var validationContext = new FluentValidation.ValidationContext<User>(EditingUser);
                var validationResult = await _validator.ValidateAsync(validationContext);

                if (!validationResult.IsValid)
                {
                    ErrorMessage = string.Join(Environment.NewLine, validationResult.Errors.Select(e => e.ErrorMessage));
                    return;
                }

                result = await _userService.AddUserAsync(EditingUser);
                if (result)
                {
                    SuccessMessage = $"Tạo tài khoản đăng nhập '{EditingUser.Username}' thành công.";
                }
            }
            else
            {
                if (_userSession.UserGroup != UserGroup.NganHang)
                {
                    ErrorMessage = "Chỉ người dùng Ngân hàng mới được đặt lại mật khẩu.";
                    return;
                }

                result = await _userService.UpdateUserAsync(EditingUser);
                if (result)
                {
                    SuccessMessage = $"Đã đặt lại mật khẩu cho tài khoản '{EditingUser.Username}'.";
                }
            }

            if (result)
            {
                await LoadUsersAsync();
                Cancel();
            }
            else
            {
                ErrorMessage = "Không thể lưu thay đổi tài khoản đăng nhập.";
            }
        });
    }

    public async Task Delete()
    {
        if (SelectedUser == null) return;

        if (SelectedUser.Username.Equals(_userSession.Username, StringComparison.OrdinalIgnoreCase))
        {
            await _dialogService.ShowWarningAsync("Bạn không thể tự xóa chính tài khoản của mình.", "Xóa người dùng");
            return;
        }

        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            await _dialogService.ShowWarningAsync("Chỉ người dùng Ngân hàng mới được xóa tài khoản đăng nhập.", "Xóa đăng nhập");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Bạn có chắc muốn xóa tài khoản đăng nhập '{SelectedUser.Username}'?",
            "Xác nhận xóa đăng nhập");

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _userService.DeleteUserAsync(SelectedUser.Username);
            if (result)
            {
                await LoadUsersAsync();
                SelectedUser = null;
                SuccessMessage = "Đã xóa tài khoản đăng nhập.";
            }
            else
            {
                ErrorMessage = "Không thể xóa tài khoản đăng nhập.";
            }
        });
    }

    public async Task Restore()
    {
        await _dialogService.ShowWarningAsync(
            "Chế độ SQL-login không hỗ trợ khôi phục. Hãy tạo lại tài khoản nếu cần.",
            "Không hỗ trợ khôi phục");
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingUser = new User();
        _selectedEditingUserGroup = default;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordValidationMessage = string.Empty;
    }

    private void ApplyRoleSpecificDefaults()
    {
        if (_userSession.UserGroup == UserGroup.ChiNhanh)
        {
            _editingUser.DefaultBranch = _userSession.SelectedBranch;
        }

        _editingUser.DefaultBranch = (_editingUser.DefaultBranch ?? string.Empty).Trim().ToUpperInvariant();

        switch (_editingUser.UserGroup)
        {
            case UserGroup.ChiNhanh:
                _editingUser.EmployeeId = string.IsNullOrWhiteSpace(_editingUser.EmployeeId)
                    ? string.Empty
                    : _editingUser.EmployeeId.Trim().ToUpperInvariant();
                _editingUser.CustomerCMND = null;
                break;

            case UserGroup.KhachHang:
                _editingUser.CustomerCMND = string.IsNullOrWhiteSpace(_editingUser.CustomerCMND)
                    ? string.Empty
                    : _editingUser.CustomerCMND.Trim();
                _editingUser.EmployeeId = null;
                break;

            default:
                _editingUser.EmployeeId = null;
                _editingUser.CustomerCMND = null;
                break;
        }
    }
}

