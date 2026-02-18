using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Infrastructure.Security;
using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class LoginViewModel : Screen
{
    private readonly IAuthService _authService;
    private readonly IUserSession _userSession;
    private readonly IBranchService _branchService;

    // Real branch codes loaded from repository (no "ALL")
    private List<string> _realBranchCodes = new();

    private string _selectedBranch = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        IUserSession userSession,
        IBranchService branchService)
    {
        _authService   = authService;
        _userSession   = userSession;
        _branchService = branchService;

        DisplayName = "Bank DDS - Login";
    }

    /// <summary>
    /// Branch codes shown in the login dropdown.
    /// Real branches are loaded from IBranchService; "ALL" is appended for NganHang selection.
    /// </summary>
    public ObservableCollection<string> Branches { get; } = new();

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

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        try
        {
            var branches = await _branchService.GetAllBranchesAsync();
            _realBranchCodes = branches.Select(b => b.MACN).ToList();

            Branches.Clear();
            foreach (var code in _realBranchCodes)
                Branches.Add(code);
            Branches.Add("ALL");   // UI-only option — never stored as MACN in DB

            // Default to first real branch if no selection has been made yet
            if (string.IsNullOrEmpty(SelectedBranch) || !Branches.Contains(SelectedBranch))
                SelectedBranch = _realBranchCodes.FirstOrDefault() ?? string.Empty;
        }
        catch
        {
            // Fallback: app still works even when branch service is unavailable at login
            if (Branches.Count == 0)
            {
                Branches.Add("BENTHANH");
                Branches.Add("TANDINH");
                Branches.Add("ALL");
                _realBranchCodes = new List<string> { "BENTHANH", "TANDINH" };
            }
            SelectedBranch = Branches.FirstOrDefault() ?? string.Empty;
        }
    }

    public async Task Login()
    {
        ErrorMessage = string.Empty;

        try
        {
            var result = await _authService.LoginAsync(UserName, Password);

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
                    // permittedBranches = all real branches + "ALL" (loaded from repository)
                    permittedBranches = _realBranchCodes.Concat(new[] { "ALL" }).ToList();
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
