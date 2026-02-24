using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Infrastructure.Security;
using Caliburn.Micro;
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

/// <summary>
/// Login form aligned to the Banking distributed model (DE3 — Ngân Hàng):
///   1. User enters SQL login + password.
///   2. Credentials are verified against Publisher (sp_DangNhap).
///   3. Branch dropdown is loaded from Publisher view_DanhSachPhanManh (TOP 2).
///   4. Role determines branch access and CRUD capability.
/// </summary>
public class LoginViewModel : Screen
{
    private readonly IAuthService _authService;
    private readonly IUserSession _userSession;
    private readonly IConnectionStringProvider _connectionStringProvider;

    // Real branch codes loaded from view_DanhSachPhanManh (MACN column)
    private List<string> _realBranchCodes = new();

    private string _selectedBranch = string.Empty;
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;

    public LoginViewModel(
        IAuthService authService,
        IUserSession userSession,
        IConnectionStringProvider connectionStringProvider)
    {
        _authService              = authService;
        _userSession              = userSession;
        _connectionStringProvider = connectionStringProvider;

        DisplayName = "Bank DDS - Login";
    }

    /// <summary>
    /// Branch codes shown in the login dropdown.
    /// Loaded from Publisher view_DanhSachPhanManh (TOP 2 subscriber branches).
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

        // Branch list is loaded BEFORE login — use a lightweight sa connection or
        // a dedicated "branch-list" login that only has SELECT on view_DanhSachPhanManh.
        // Here we use the Publisher template with a temporary sa/123 credential pair
        // (same approach as the Banking Form_DangNhap which loads branch list before auth).
        await LoadBranchesFromPublisherAsync();
    }

    /// <summary>
    /// Queries view_DanhSachPhanManh on the Publisher to populate the branch dropdown.
    /// Fallback to hardcoded values if the Publisher is unreachable at startup.
    /// </summary>
    private async Task LoadBranchesFromPublisherAsync()
    {
        try
        {
            // Pre-auth connection: use sa credentials to read branch list
            // (view_DanhSachPhanManh is SELECT-accessible to all roles)
            var connStr = _connectionStringProvider.GetPublisherConnectionForLogin("sa", "123");

            await using var connection = new SqlConnection(connStr);
            await connection.OpenAsync();

            await using var cmd = new SqlCommand(
                "SELECT MACN, TENCN FROM view_DanhSachPhanManh", connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            _realBranchCodes.Clear();
            Branches.Clear();

            while (await reader.ReadAsync())
            {
                var macn = reader["MACN"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(macn))
                {
                    _realBranchCodes.Add(macn);
                    Branches.Add(macn);
                }
            }

            if (string.IsNullOrEmpty(SelectedBranch) || !Branches.Contains(SelectedBranch))
                SelectedBranch = _realBranchCodes.FirstOrDefault() ?? string.Empty;
        }
        catch
        {
            // Fallback when Publisher is unreachable
            if (Branches.Count == 0)
            {
                Branches.Add("BENTHANH");
                Branches.Add("TANDINH");
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

            // Determine permitted branches and effective branch based on role
            var permittedBranches = new List<string>();
            UserGroup userGroup;

            switch (result.UserGroup)
            {
                case "NganHang":
                    userGroup = UserGroup.NganHang;
                    // NganHang can VIEW all branches but cannot CRUD.
                    // No "ALL" pseudo-branch — user picks a real branch to view.
                    permittedBranches = new List<string>(_realBranchCodes);
                    break;

                case "ChiNhanh":
                    userGroup = UserGroup.ChiNhanh;
                    // ChiNhanh is locked to their own branch, full CRUD.
                    // DefaultBranch comes from sp_DangNhap (MACN column).
                    // Fallback: use the branch the user selected in the login dropdown
                    // when the SP could not resolve it (e.g. no NHANVIEN row yet).
                    var chiNhanhBranch = !string.IsNullOrEmpty(result.DefaultBranch)
                        ? result.DefaultBranch
                        : SelectedBranch;
                    permittedBranches = new List<string> { chiNhanhBranch };
                    SelectedBranch = chiNhanhBranch;
                    break;

                case "KhachHang":
                    userGroup = UserGroup.KhachHang;
                    // KhachHang can only view own statement/report.
                    // Same fallback strategy as ChiNhanh.
                    var khachHangBranch = !string.IsNullOrEmpty(result.DefaultBranch)
                        ? result.DefaultBranch
                        : SelectedBranch;
                    permittedBranches = new List<string> { khachHangBranch };
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
