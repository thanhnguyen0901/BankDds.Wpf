using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Infrastructure.Security;
using Caliburn.Micro;

namespace BankDds.Wpf.ViewModels;

public class LoginViewModel : Screen
{
    private readonly IAuthService _authService;
    private readonly IUserSession _userSession;

    private string _selectedBranch = "BENTHANH";
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        IUserSession userSession)
    {
        _authService = authService;
        _userSession = userSession;

        DisplayName = "Bank DDS - Login";
        
        Branches = new List<string> { "BENTHANH", "TANDINH", "ALL" };
    }

    public List<string> Branches { get; }

    public string SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            _selectedBranch = value;
            NotifyOfPropertyChange(() => SelectedBranch);
        }
    }

    public string UserName
    {
        get => _userName;
        set
        {
            _userName = value;
            NotifyOfPropertyChange(() => UserName);
            NotifyOfPropertyChange(() => CanLogin);
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value;
            NotifyOfPropertyChange(() => Password);
            NotifyOfPropertyChange(() => CanLogin);
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

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanLogin => !string.IsNullOrWhiteSpace(UserName);

    public async Task Login()
    {
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authService.LoginAsync("SERVER", UserName, Password);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Login failed";
                return;
            }

            // Determine permitted branches based on user group
            var permittedBranches = new List<string>();
            UserGroup userGroup;

            switch (result.UserGroup)
            {
                case "NganHang":
                    userGroup = UserGroup.NganHang;
                    permittedBranches = new List<string> { "BENTHANH", "TANDINH", "ALL" };
                    break;
                case "ChiNhanh":
                    userGroup = UserGroup.ChiNhanh;
                    permittedBranches = new List<string> { result.DefaultBranch };
                    SelectedBranch = result.DefaultBranch; // Force branch selection
                    break;
                case "KhachHang":
                    userGroup = UserGroup.KhachHang;
                    permittedBranches = new List<string> { result.DefaultBranch };
                    SelectedBranch = result.DefaultBranch;
                    break;
                default:
                    ErrorMessage = "Unknown user group";
                    return;
            }

            // Set user session with employee ID
            _userSession.SetSession(
                UserName,
                UserName, // Use username as display name
                userGroup,
                SelectedBranch,
                permittedBranches,
                result.CustomerCMND,
                result.EmployeeId);

            // Navigate to Home through parent conductor
            if (Parent is MainShellViewModel mainShell)
            {
                await mainShell.ShowHomeAsync();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }
    }
}
