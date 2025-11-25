using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Wpf.ViewModels;

public class HomeViewModel : Conductor<Screen>.Collection.OneActive
{
    private readonly IUserSession _userSession;

    public HomeViewModel(IUserSession userSession)
    {
        _userSession = userSession;
        base.DisplayName = "Home";
    }

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
    public bool CanViewCustomers => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
    public bool CanViewAccounts => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
    public bool CanViewEmployees => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
    public bool CanViewTransactions => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
    public bool CanViewReports => true; // All users can view reports (filtered by role)
    public bool CanViewAdmin => _userSession.UserGroup == UserGroup.NganHang;
    public bool IsCustomerMode => _userSession.UserGroup == UserGroup.KhachHang;

    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        RefreshPermissions();
        return base.OnActivateAsync(cancellationToken);
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
        NotifyOfPropertyChange(() => IsCustomerMode);
    }

    public async Task ShowCustomers()
    {
        if (!CanViewCustomers) return;
        
        var vm = IoC.Get<CustomersViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowAccounts()
    {
        if (!CanViewAccounts) return;
        
        var vm = IoC.Get<AccountsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowEmployees()
    {
        if (!CanViewEmployees) return;
        
        var vm = IoC.Get<EmployeesViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowTransactions()
    {
        if (!CanViewTransactions) return;
        
        var vm = IoC.Get<TransactionsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowReports()
    {
        var vm = IoC.Get<ReportsViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task ShowAdmin()
    {
        if (!CanViewAdmin) return;
        
        var vm = IoC.Get<AdminViewModel>();
        await ActivateItemAsync(vm, cancellationToken: default);
    }

    public async Task Logout()
    {
        _userSession.ClearSession();
        
        if (Parent is MainShellViewModel mainShell)
        {
            await mainShell.ShowLoginAsync();
        }
    }
}
