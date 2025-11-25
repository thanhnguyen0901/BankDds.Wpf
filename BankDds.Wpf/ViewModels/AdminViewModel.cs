using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Wpf.Helpers;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class AdminViewModel : Screen
{
    private readonly IUserService _userService;
    private readonly IUserSession _userSession;
    
    private ObservableCollection<User> _users = new();
    private User? _selectedUser;
    private User _editingUser = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;

    public AdminViewModel(IUserService userService, IUserSession userSession)
    {
        _userService = userService;
        _userSession = userSession;
        DisplayName = "User Administration";
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
            NotifyOfPropertyChange(() => CanSave);
            NotifyOfPropertyChange(() => CanCancel);
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyOfPropertyChange(() => ErrorMessage);
            NotifyOfPropertyChange(() => HasError);
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ObservableCollection<string> AvailableUserGroups { get; } = new() { "NganHang", "ChiNhanh", "KhachHang" };
    public ObservableCollection<string> AvailableBranches { get; } = new() { "BENTHANH", "TANDINH", "ALL" };

    // CanExecute properties - Standard CRUD pattern
    public bool CanAdd => !IsEditing;
    public bool CanEdit => SelectedUser != null && !IsEditing;
    public bool CanDelete => SelectedUser != null && !IsEditing;
    public bool CanSave => IsEditing && 
                           !string.IsNullOrWhiteSpace(EditingUser.Username) && 
                           !string.IsNullOrWhiteSpace(EditingUser.Password);
    public bool CanCancel => IsEditing;

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        if (_userSession.UserGroup != UserGroup.NganHang)
        {
            ErrorMessage = "Access Denied: Only Bank-level administrators can access this module.";
            return;
        }
        
        await LoadUsersAsync();
    }

    private async Task LoadUsersAsync()
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();
            Users = new ObservableCollection<User>(users);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading users: {ex.Message}";
        }
    }

    public void Add()
    {
        EditingUser = new User
        {
            UserGroup = UserGroup.ChiNhanh,
            DefaultBranch = "BENTHANH"
        };
        IsEditing = true;
        SelectedUser = null;
        ErrorMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedUser == null) return;

        EditingUser = new User
        {
            Username = SelectedUser.Username,
            Password = SelectedUser.Password,
            UserGroup = SelectedUser.UserGroup,
            DefaultBranch = SelectedUser.DefaultBranch,
            CustomerCMND = SelectedUser.CustomerCMND
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public async Task Save()
    {
        try
        {
            if (_userSession.UserGroup != UserGroup.NganHang && 
                EditingUser.UserGroup == UserGroup.NganHang)
            {
                ErrorMessage = "Only Bank administrators can create Bank-level users.";
                return;
            }

            if (EditingUser.UserGroup == UserGroup.KhachHang && 
                string.IsNullOrWhiteSpace(EditingUser.CustomerCMND))
            {
                ErrorMessage = "CustomerCMND is required for customer users.";
                return;
            }

            bool result;

            if (SelectedUser == null)
            {
                result = await _userService.AddUserAsync(EditingUser);
            }
            else
            {
                result = await _userService.UpdateUserAsync(EditingUser);
            }

            if (result)
            {
                IsEditing = false;
                await LoadUsersAsync();
                SelectedUser = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to save user. Username may already exist.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving user: {ex.Message}";
        }
    }

    public async Task Delete()
    {
        if (SelectedUser == null) return;

        if (SelectedUser.Username.Equals(_userSession.Username, StringComparison.OrdinalIgnoreCase))
        {
            DialogHelper.ShowWarning("Cannot delete your own account.", "Delete User");
            return;
        }

        // Show confirmation dialog
        var confirmed = DialogHelper.ShowConfirmation(
            $"Are you sure you want to delete user '{SelectedUser.Username}'?",
            "Delete Confirmation"
        );

        if (!confirmed) return;

        try
        {
            var result = await _userService.DeleteUserAsync(SelectedUser.Username);
            if (result)
            {
                await LoadUsersAsync();
                SelectedUser = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to delete user.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting user: {ex.Message}";
        }
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingUser = new User();
        ErrorMessage = string.Empty;
    }
}
