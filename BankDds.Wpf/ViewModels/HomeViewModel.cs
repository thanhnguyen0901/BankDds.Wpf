using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class HomeViewModel : Conductor<Screen>.Collection.OneActive
{
    private readonly IUserSession _userSession;

    // Tracks which "Show*" method created the current ActiveItem so we can
    // re-invoke it after a branch switch to refresh data.
    private Func<Task>? _lastShowAction;

    public HomeViewModel(IUserSession userSession)
    {
        _userSession = userSession;
        base.DisplayName = "Home";
    }

    // ─────────────────────── Branch switching (NGANHANG only) ───────────────────────

    /// <summary>
    /// Branch codes available in the dropdown. Populated from
    /// <see cref="IUserSession.PermittedBranches"/> on activation.
    /// </summary>
    public ObservableCollection<string> Branches { get; } = new();

    /// <summary>
    /// True when the logged-in user may switch branches (NGANHANG role only).
    /// Bound to the branch ComboBox visibility in HomeView.
    /// </summary>
    public bool CanSwitchBranch => _userSession.UserGroup == UserGroup.NganHang;

    private string _selectedBranchCode = string.Empty;

    /// <summary>
    /// Two-way bound to the branch ComboBox. Changing the value updates
    /// <see cref="IUserSession.SelectedBranch"/> and refreshes the active child view.
    /// </summary>
    public string SelectedBranchCode
    {
        get => _selectedBranchCode;
        set
        {
            if (_selectedBranchCode == value) return;
            _selectedBranchCode = value;
            NotifyOfPropertyChange(() => SelectedBranchCode);

            // Propagate to session — this triggers SelectedBranchChanged event
            // which is handled by OnBranchChanged below.
            if (!string.IsNullOrEmpty(value) && _userSession.PermittedBranches.Contains(value))
            {
                _userSession.SetSelectedBranch(value);
            }
        }
    }

    // ─────────────────────── User info ───────────────────────

    public string Username => _userSession.Username;
    public string UserDisplayName => _userSession.DisplayName;
    public string SelectedBranch => _userSession.SelectedBranch;

    public string RoleText
    {
        get
        {
            return _userSession.UserGroup switch
            {
                UserGroup.NganHang => "Ngan Hang (Bank Level)",
                UserGroup.ChiNhanh => $"Chi Nhanh ({SelectedBranch})",
                UserGroup.KhachHang => "Khach Hang (Customer)",
                _ => "Unknown"
            };
        }
    }

    // Role-based permissions
    public bool CanViewCustomers => _userSession.UserGroup == UserGroup.ChiNhanh;
    public bool CanViewAccounts => _userSession.UserGroup == UserGroup.ChiNhanh;
    public bool CanViewEmployees => _userSession.UserGroup == UserGroup.ChiNhanh;
    public bool CanViewTransactions => _userSession.UserGroup == UserGroup.ChiNhanh;
    public bool CanViewReports => true; // All users can view reports (filtered by role)
    // GAP-01: ChiNhanh must also reach Admin to create logins in the same group (DE3 §IV.2)
    public bool CanViewAdmin => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
    // GAP-04: Branch management — NganHang only
    public bool CanViewBranches => _userSession.UserGroup == UserGroup.NganHang;
    // Cross-branch customer lookup — NganHang only
    public bool CanViewCustomerLookup => _userSession.UserGroup == UserGroup.NganHang;
    public bool IsCustomerMode => _userSession.UserGroup == UserGroup.KhachHang;

    // ─────────────────────── Lifecycle ───────────────────────

    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Populate branch dropdown from session
        Branches.Clear();
        foreach (var code in _userSession.PermittedBranches)
            Branches.Add(code);

        // Sync local selection with session (suppress re-entry by setting field directly)
        _selectedBranchCode = _userSession.SelectedBranch;
        NotifyOfPropertyChange(() => SelectedBranchCode);

        // Subscribe to branch-change events (e.g. if another component changes it)
        _userSession.SelectedBranchChanged += OnBranchChanged;

        RefreshPermissions();
        return base.OnActivateAsync(cancellationToken);
    }

    protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
    {
        _userSession.SelectedBranchChanged -= OnBranchChanged;
        return base.OnDeactivateAsync(close, cancellationToken);
    }

    /// <summary>
    /// Called when <see cref="IUserSession.SelectedBranch"/> changes.
    /// Closes the current child view and re-opens it so it loads fresh
    /// data from the newly selected branch connection.
    /// </summary>
    private async void OnBranchChanged()
    {
        // Keep local selection in sync (field-only to avoid re-entry)
        _selectedBranchCode = _userSession.SelectedBranch;
        NotifyOfPropertyChange(() => SelectedBranchCode);
        NotifyOfPropertyChange(() => SelectedBranch);
        NotifyOfPropertyChange(() => RoleText);

        // Refresh the active child screen by closing it and re-invoking
        // the same Show* action, which creates a new VM instance and
        // triggers OnActivateAsync → fresh data load.
        if (_lastShowAction != null)
        {
            var action = _lastShowAction;
            await DeactivateItemAsync(ActiveItem, close: true, cancellationToken: default);
            await action();
        }
    }

    private void RefreshPermissions()
    {
        NotifyOfPropertyChange(() => Username);
        NotifyOfPropertyChange(() => UserDisplayName);
        NotifyOfPropertyChange(() => SelectedBranch);
        NotifyOfPropertyChange(() => RoleText);
        NotifyOfPropertyChange(() => CanViewCustomers);
        NotifyOfPropertyChange(() => CanViewAccounts);
        NotifyOfPropertyChange(() => CanViewEmployees);
        NotifyOfPropertyChange(() => CanViewTransactions);
        NotifyOfPropertyChange(() => CanViewReports);
        NotifyOfPropertyChange(() => CanViewAdmin);
        NotifyOfPropertyChange(() => CanViewBranches);
        NotifyOfPropertyChange(() => CanViewCustomerLookup);
        NotifyOfPropertyChange(() => CanSwitchBranch);
        NotifyOfPropertyChange(() => IsCustomerMode);
    }

    // ─────────────────────── Navigation actions ───────────────────────

    public async Task ShowCustomers()
    {
        if (!CanViewCustomers) return;
        _lastShowAction = ShowCustomers;
        var vm = IoC.Get<CustomersViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowAccounts()
    {
        if (!CanViewAccounts) return;
        _lastShowAction = ShowAccounts;
        var vm = IoC.Get<AccountsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowEmployees()
    {
        if (!CanViewEmployees) return;
        _lastShowAction = ShowEmployees;
        var vm = IoC.Get<EmployeesViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowTransactions()
    {
        if (!CanViewTransactions) return;
        _lastShowAction = ShowTransactions;
        var vm = IoC.Get<TransactionsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowReports()
    {
        _lastShowAction = ShowReports;
        var vm = IoC.Get<ReportsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowAdmin()
    {
        if (!CanViewAdmin) return;
        _lastShowAction = ShowAdmin;
        var vm = IoC.Get<AdminViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowBranches()
    {
        if (!CanViewBranches) return;
        _lastShowAction = ShowBranches;
        var vm = IoC.Get<BranchesViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowCustomerLookup()
    {
        if (!CanViewCustomerLookup) return;
        _lastShowAction = ShowCustomerLookup;
        var vm = IoC.Get<CustomerLookupViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task Logout()
    {
        _lastShowAction = null;
        _userSession.ClearSession();
        
        if (Parent is MainShellViewModel mainShell)
        {
            await mainShell.ShowLoginAsync();
        }
    }
}


