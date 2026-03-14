using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Infrastructure.Security;
using Caliburn.Micro;

namespace BankDds.Wpf.ViewModels;

/// <summary>
/// Login flow:
///   1. User enters SQL login + password.
///   2. Credentials are verified on Publisher via sp_DangNhap.
///   3. SQL returns role + branch scope (MACN) for the session.
///   4. After login, NganHang branch list is loaded from DB for post-login switching.
/// </summary>
public class LoginViewModel : Screen
{
    private readonly IAuthService _authService;
    private readonly IBranchService _branchService;
    private readonly IUserSession _userSession;
    private readonly IConnectionStringProvider _connectionStringProvider;

    private List<string> _realBranchCodes = new();

    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        IBranchService branchService,
        IUserSession userSession,
        IConnectionStringProvider connectionStringProvider)
    {
        _authService = authService;
        _branchService = branchService;
        _userSession = userSession;
        _connectionStringProvider = connectionStringProvider;

        DisplayName = "Bank DDS - Login";
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

    public bool CanLogin =>
        !string.IsNullOrWhiteSpace(UserName) &&
        !string.IsNullOrWhiteSpace(Password);

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        LoadConfiguredBranches();
    }

    private void LoadConfiguredBranches()
    {
        _realBranchCodes = _connectionStringProvider.GetConfiguredBranchCodes().ToList();
        if (_realBranchCodes.Count == 0)
        {
            _realBranchCodes =
            [
                _connectionStringProvider.DefaultBranch.Trim().ToUpperInvariant()
            ];
        }

        _realBranchCodes = _realBranchCodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<List<string>> LoadPermittedBranchesForNganHangAsync()
    {
        try
        {
            var branches = await _branchService.GetAllBranchesAsync();
            var codes = branches
                .Select(b => b.MACN.Trim().ToUpperInvariant())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (codes.Count > 0)
            {
                _realBranchCodes = codes;
            }
        }
        catch
        {
            // Keep configured branch list if DB is unavailable.
        }

        return _realBranchCodes.Count > 0
            ? new List<string>(_realBranchCodes)
            :
            [
                _connectionStringProvider.DefaultBranch.Trim().ToUpperInvariant()
            ];
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

            var permittedBranches = new List<string>();
            UserGroup userGroup;
            string selectedBranch;

            switch (result.UserGroup)
            {
                case "NganHang":
                    userGroup = UserGroup.NganHang;
                    permittedBranches = await LoadPermittedBranchesForNganHangAsync();
                    selectedBranch = result.DefaultBranch?.Trim().ToUpperInvariant() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(selectedBranch) ||
                        !permittedBranches.Contains(selectedBranch, StringComparer.OrdinalIgnoreCase))
                    {
                        selectedBranch = permittedBranches.First();
                    }
                    break;

                case "ChiNhanh":
                    userGroup = UserGroup.ChiNhanh;
                    if (string.IsNullOrWhiteSpace(result.DefaultBranch))
                    {
                        ErrorMessage = "Account is missing branch mapping (MACN). Please contact administrator.";
                        return;
                    }
                    selectedBranch = result.DefaultBranch.Trim().ToUpperInvariant();
                    permittedBranches = [selectedBranch];
                    break;

                case "KhachHang":
                    userGroup = UserGroup.KhachHang;
                    if (string.IsNullOrWhiteSpace(result.DefaultBranch))
                    {
                        ErrorMessage = "Customer account is missing branch mapping (MACN). Please contact administrator.";
                        return;
                    }
                    if (string.IsNullOrWhiteSpace(result.CustomerCMND))
                    {
                        ErrorMessage = "Customer account is missing CMND mapping. Please contact administrator.";
                        return;
                    }
                    selectedBranch = result.DefaultBranch.Trim().ToUpperInvariant();
                    permittedBranches = [selectedBranch];
                    break;

                default:
                    ErrorMessage = "Unknown user group";
                    return;
            }

            var displayName = result.DisplayName ?? UserName;

            _userSession.SetSession(
                UserName,
                displayName,
                userGroup,
                selectedBranch,
                permittedBranches,
                result.CustomerCMND,
                result.EmployeeId);

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
