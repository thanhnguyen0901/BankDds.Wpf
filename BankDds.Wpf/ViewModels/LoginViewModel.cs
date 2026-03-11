using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Infrastructure.Security;
using Caliburn.Micro;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

/// <summary>
/// Login flow:
///   1. User enters SQL login + password.
///   2. Credentials are verified on Publisher via sp_DangNhap.
///   3. Branch list shown pre-login comes from appsettings Branch_* keys (no hardcoded credentials).
///   4. After login, NganHang branch list is refreshed from DB via BranchService.
/// </summary>
public class LoginViewModel : Screen
{
    private readonly IAuthService _authService;
    private readonly IBranchService _branchService;
    private readonly IUserSession _userSession;
    private readonly IConnectionStringProvider _connectionStringProvider;

    private List<string> _realBranchCodes = new();

    private string _selectedBranch = string.Empty;
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

        Branches.Clear();
        foreach (var macn in _realBranchCodes)
            Branches.Add(macn);

        if (string.IsNullOrWhiteSpace(SelectedBranch) ||
            !_realBranchCodes.Contains(SelectedBranch, StringComparer.OrdinalIgnoreCase))
            SelectedBranch = _realBranchCodes.First();
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
                Branches.Clear();
                foreach (var code in codes)
                    Branches.Add(code);
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

            switch (result.UserGroup)
            {
                case "NganHang":
                    userGroup = UserGroup.NganHang;
                    permittedBranches = await LoadPermittedBranchesForNganHangAsync();
                    if (string.IsNullOrWhiteSpace(SelectedBranch) ||
                        !permittedBranches.Contains(SelectedBranch, StringComparer.OrdinalIgnoreCase))
                    {
                        SelectedBranch = permittedBranches.First();
                    }
                    break;

                case "ChiNhanh":
                    userGroup = UserGroup.ChiNhanh;
                    var chiNhanhBranch = !string.IsNullOrWhiteSpace(result.DefaultBranch)
                        ? result.DefaultBranch
                        : (!string.IsNullOrWhiteSpace(SelectedBranch)
                            ? SelectedBranch
                            : _connectionStringProvider.DefaultBranch);
                    chiNhanhBranch = chiNhanhBranch.Trim().ToUpperInvariant();
                    permittedBranches = [chiNhanhBranch];
                    SelectedBranch = chiNhanhBranch;
                    break;

                case "KhachHang":
                    userGroup = UserGroup.KhachHang;
                    var khachHangBranch = !string.IsNullOrWhiteSpace(result.DefaultBranch)
                        ? result.DefaultBranch
                        : (!string.IsNullOrWhiteSpace(SelectedBranch)
                            ? SelectedBranch
                            : _connectionStringProvider.DefaultBranch);
                    khachHangBranch = khachHangBranch.Trim().ToUpperInvariant();
                    permittedBranches = [khachHangBranch];
                    SelectedBranch = khachHangBranch;
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
                SelectedBranch,
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
