using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class AccountsViewModel : Screen
{
    private readonly IAccountService _accountService;
    private readonly ICustomerService _customerService;
    private readonly IUserSession _userSession;
    
    private ObservableCollection<Account> _accounts = new();
    private Account? _selectedAccount;

    public AccountsViewModel(IAccountService accountService, ICustomerService customerService, IUserSession userSession)
    {
        _accountService = accountService;
        _customerService = customerService;
        _userSession = userSession;
        DisplayName = "Account Management";
    }

    public ObservableCollection<Account> Accounts
    {
        get => _accounts;
        set
        {
            _accounts = value;
            NotifyOfPropertyChange(() => Accounts);
        }
    }

    public Account? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            _selectedAccount = value;
            NotifyOfPropertyChange(() => SelectedAccount);
        }
    }

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadAccountsAsync();
    }

    private async Task LoadAccountsAsync()
    {
        List<Account> accounts;
        
        if (_userSession.UserGroup == UserGroup.NganHang)
        {
            accounts = await _accountService.GetAllAccountsAsync();
        }
        else if (_userSession.UserGroup == UserGroup.KhachHang)
        {
            accounts = await _accountService.GetAccountsByCustomerAsync(_userSession.CustomerCMND ?? "");
        }
        else
        {
            accounts = await _accountService.GetAccountsByBranchAsync(_userSession.SelectedBranch);
        }

        Accounts = new ObservableCollection<Account>(accounts);
    }
}
