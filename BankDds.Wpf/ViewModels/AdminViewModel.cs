using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;

using System.Collections.ObjectModel;

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
        _userService   = userService;
        _userSession   = userSession;
        _dialogService = dialogService;
        _validator     = validator;
        _branchService = branchService;
        DisplayName    = "User Administration";
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
        }
    }

    public User EditingUser
    {
        get => _editingUser;
        set
        {
            _editingUser = value;
            NotifyOfPropertyChange(() => EditingUser);
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

    public bool IsPasswordValid => PasswordValidationMessage.StartsWith("?");

    public ObservableCollection<UserGroup> AvailableUserGroups { get; } = new();
    /// <summary>
    /// Real branch codes loaded from IBranchService on activate.
    /// Does NOT include "ALL" — DefaultBranch must be a real branch code.
    /// </summary>
    public ObservableCollection<string> AvailableBranches { get; } = new();

    // CanExecute properties - Standard CRUD pattern
    public bool CanAdd => !IsEditing;
    // In SQL-login mode, password reset and login deletion are NGANHANG-only actions.
    public bool CanEdit => SelectedUser != null && !IsEditing && _userSession.UserGroup == UserGroup.NganHang;
    public bool CanDelete  => SelectedUser != null && !IsEditing && _userSession.UserGroup == UserGroup.NganHang;
    public bool CanRestore => false; // Soft-restore is not supported after moving to SQL login lifecycle.
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
            PasswordValidationMessage = "Password is required";
            return;
        }
        
        if (NewPassword.Length < 8)
        {
            PasswordValidationMessage = "Password must be at least 8 characters";
            return;
        }
        
        if (!NewPassword.Any(char.IsUpper))
        {
            PasswordValidationMessage = "Password must contain at least one uppercase letter";
            return;
        }
        
        if (!NewPassword.Any(char.IsLower))
        {
            PasswordValidationMessage = "Password must contain at least one lowercase letter";
            return;
        }
        
        if (!NewPassword.Any(char.IsDigit))
        {
            PasswordValidationMessage = "Password must contain at least one number";
            return;
        }
        
        if (NewPassword != ConfirmPassword)
        {
            PasswordValidationMessage = "Passwords do not match";
            return;
        }
        
        PasswordValidationMessage = "? Password meets requirements";
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        // Allow both NganHang and ChiNhanh to access admin (with different privileges)
        if (_userSession.UserGroup != UserGroup.NganHang && _userSession.UserGroup != UserGroup.ChiNhanh)
        {
            ErrorMessage = "Access Denied: Only Bank-level and Branch-level administrators can access this module.";
            return;
        }

        // Populate creatable user groups based on the same-group rule.
        AvailableUserGroups.Clear();
        if (_userSession.UserGroup == UserGroup.NganHang)
        {
            AvailableUserGroups.Add(UserGroup.NganHang);
        }
        else // ChiNhanh
        {
            AvailableUserGroups.Add(UserGroup.ChiNhanh);
        }
        NotifyOfPropertyChange(() => AvailableUserGroups);

        // Load real branch codes from repository — no "ALL" here; DefaultBranch must be a real branch
        try
        {
            var branches = await _branchService.GetAllBranchesAsync();
            AvailableBranches.Clear();
            foreach (var b in branches)
                AvailableBranches.Add(b.MACN);
        }
        catch
        {
            // Fallback: keep existing entries if service is unavailable
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
        EditingUser = new User
        {
            // Same-group default based on current admin role.
            UserGroup = _userSession.UserGroup == UserGroup.NganHang
                ? UserGroup.NganHang
                : UserGroup.ChiNhanh,
            // Always default the branch to the current user's branch so the service-layer
            // RequireCanManageUserInBranch check passes without the admin needing to change it
            DefaultBranch = _userSession.SelectedBranch == "ALL"
                ? (AvailableBranches.FirstOrDefault() ?? "BENTHANH")
                : _userSession.SelectedBranch,
            // EmployeeId links this user account to an existing employee record.
            // It must be entered manually by the administrator — auto-generation here
            // would produce a dangling reference to a non-existent employee.
            EmployeeId = string.Empty
        };
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
            ErrorMessage = "Only Bank administrators can reset passwords in this module.";
            return;
        }

        EditingUser = new User
        {
            Username      = SelectedUser.Username,
            PasswordHash  = SelectedUser.PasswordHash,
            UserGroup     = SelectedUser.UserGroup,
            DefaultBranch = SelectedUser.DefaultBranch,
            CustomerCMND  = SelectedUser.CustomerCMND,
            EmployeeId    = SelectedUser.EmployeeId,
            TrangThaiXoa  = SelectedUser.TrangThaiXoa  // preserve soft-delete state
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordValidationMessage = "Enter new password for reset.";
    }

    public async Task Save()
    {
        // Validation before loading indicator
        if (_userSession.UserGroup == UserGroup.NganHang &&
            EditingUser.UserGroup != UserGroup.NganHang)
        {
            ErrorMessage = "Bank administrators can only create Bank-level (NganHang) users.";
            return;
        }

        if (_userSession.UserGroup != UserGroup.NganHang &&
            EditingUser.UserGroup == UserGroup.NganHang)
        {
            ErrorMessage = "Only Bank administrators can create Bank-level users.";
            return;
        }

        if (_userSession.UserGroup == UserGroup.ChiNhanh && 
            EditingUser.UserGroup != UserGroup.ChiNhanh)
        {
            ErrorMessage = "Branch administrators can only create Branch-level (ChiNhanh) users.";
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            // In SQL-login mode PasswordHash carries plain password input for SP calls.
            EditingUser.PasswordHash = NewPassword;

            // Validate input model.
            var validationContext = new FluentValidation.ValidationContext<User>(EditingUser);

            var validationResult = await _validator.ValidateAsync(validationContext);
            if (!validationResult.IsValid)
            {
                // Aggregate all validation errors
                ErrorMessage = string.Join(Environment.NewLine, 
                    validationResult.Errors.Select(e => e.ErrorMessage));
                return;
            }

            bool result;

            if (SelectedUser == null)
            {
                result = await _userService.AddUserAsync(EditingUser);
                if (result)
                {
                    SuccessMessage = $"Login '{EditingUser.Username}' created successfully.";
                }
            }
            else
            {
                if (_userSession.UserGroup != UserGroup.NganHang)
                {
                    ErrorMessage = "Only Bank administrators can reset passwords.";
                    return;
                }
                result = await _userService.UpdateUserAsync(EditingUser);
                if (result)
                {
                    SuccessMessage = $"Password for '{EditingUser.Username}' has been reset.";
                }
            }

            if (result)
            {
                await LoadUsersAsync();
                Cancel();
            }
            else
            {
                ErrorMessage = "Failed to save login changes.";
            }
        });
    }

    public async Task Delete()
    {
        if (SelectedUser == null) return;

        if (SelectedUser.Username.Equals(_userSession.Username, StringComparison.OrdinalIgnoreCase))
        {
            await _dialogService.ShowWarningAsync("Cannot delete your own account.", "Delete User");
            return;
        }

        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            await _dialogService.ShowWarningAsync("Only Bank administrators can delete logins.", "Delete Login");
            return;
        }

        // Hard delete SQL login/user via sp_XoaTaiKhoan
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Bạn có chắc chắn muốn xóa login '{SelectedUser.Username}'?\nThao tác này sẽ xóa DB user và SQL login.",
            "Xác nhận xóa login"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _userService.DeleteUserAsync(SelectedUser.Username);
            if (result)
            {
                await LoadUsersAsync();
                SelectedUser = null;
                SuccessMessage = "Login đã được xóa.";
            }
            else
            {
                ErrorMessage = "Không thể xóa login.";
            }
        });
    }

    public async Task Restore()
    {
        await _dialogService.ShowWarningAsync(
            "Restore is not supported in SQL-login mode. Please recreate the login if needed.",
            "Restore Not Supported");
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingUser = new User();
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        NewPassword = string.Empty;
        ConfirmPassword = string.Empty;
        PasswordValidationMessage = string.Empty;
    }
}
