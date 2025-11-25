using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Wpf.Helpers;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class AccountsViewModel : Screen
{
    private readonly IAccountService _accountService;
    private readonly ICustomerService _customerService;
    private readonly IUserSession _userSession;
    
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private ObservableCollection<Account> _accounts = new();
    private Account? _selectedAccount;
    private Account _editingAccount = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;

    public AccountsViewModel(IAccountService accountService, ICustomerService customerService, IUserSession userSession)
    {
        _accountService = accountService;
        _customerService = customerService;
        _userSession = userSession;
        DisplayName = "Account Management";
    }

    public ObservableCollection<Customer> Customers
    {
        get => _customers;
        set
        {
            _customers = value;
            NotifyOfPropertyChange(() => Customers);
        }
    }

    public Customer? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            _selectedCustomer = value;
            NotifyOfPropertyChange(() => SelectedCustomer);
            NotifyOfPropertyChange(() => CanAdd);
            _ = LoadAccountsForCustomerAsync();
        }
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
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
        }
    }

    public Account EditingAccount
    {
        get => _editingAccount;
        set
        {
            _editingAccount = value;
            NotifyOfPropertyChange(() => EditingAccount);
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

    // CanExecute properties - Standard CRUD pattern
    public bool CanAdd => SelectedCustomer != null && !IsEditing;
    public bool CanEdit => SelectedAccount != null && !IsEditing;
    public bool CanDelete => SelectedAccount != null && !IsEditing;
    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingAccount.SOTK) && SelectedCustomer != null;
    public bool CanCancel => IsEditing;

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            List<Customer> customers;
            
            if (_userSession.UserGroup == UserGroup.NganHang)
            {
                customers = await _customerService.GetAllCustomersAsync();
            }
            else if (_userSession.UserGroup == UserGroup.KhachHang)
            {
                var customer = await _customerService.GetCustomerByCMNDAsync(_userSession.CustomerCMND ?? "");
                customers = customer != null ? new List<Customer> { customer } : new List<Customer>();
            }
            else
            {
                customers = await _customerService.GetCustomersByBranchAsync(_userSession.SelectedBranch);
            }

            Customers = new ObservableCollection<Customer>(customers);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading customers: {ex.Message}";
        }
    }

    private async Task LoadAccountsForCustomerAsync()
    {
        if (SelectedCustomer == null)
        {
            Accounts = new ObservableCollection<Account>();
            return;
        }

        try
        {
            var accounts = await _accountService.GetAccountsByCustomerAsync(SelectedCustomer.CMND);
            Accounts = new ObservableCollection<Account>(accounts);
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading accounts: {ex.Message}";
        }
    }

    public void Add()
    {
        if (SelectedCustomer == null) return;

        EditingAccount = new Account
        {
            CMND = SelectedCustomer.CMND,
            MACN = SelectedCustomer.MaCN,
            SODU = 0,
            NGAYMOTK = DateTime.Now,
            SOTK = GenerateAccountNumber()
        };
        IsEditing = true;
        SelectedAccount = null;
        ErrorMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedAccount == null) return;

        EditingAccount = new Account
        {
            SOTK = SelectedAccount.SOTK,
            CMND = SelectedAccount.CMND,
            SODU = SelectedAccount.SODU,
            MACN = SelectedAccount.MACN,
            NGAYMOTK = SelectedAccount.NGAYMOTK
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public async Task Save()
    {
        try
        {
            bool result;

            if (SelectedAccount == null)
            {
                result = await _accountService.AddAccountAsync(EditingAccount);
            }
            else
            {
                result = await _accountService.UpdateAccountAsync(EditingAccount);
            }

            if (result)
            {
                IsEditing = false;
                await LoadAccountsForCustomerAsync();
                SelectedAccount = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to save account.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error saving account: {ex.Message}";
        }
    }

    public async Task Delete()
    {
        if (SelectedAccount == null) return;

        if (SelectedAccount.SODU != 0)
        {
            DialogHelper.ShowWarning("Cannot delete account with non-zero balance.", "Delete Account");
            return;
        }

        // Show confirmation dialog
        var confirmed = DialogHelper.ShowConfirmation(
            $"Are you sure you want to delete account '{SelectedAccount.SOTK}'?",
            "Delete Confirmation"
        );

        if (!confirmed) return;

        try
        {
            var result = await _accountService.DeleteAccountAsync(SelectedAccount.SOTK);
            if (result)
            {
                await LoadAccountsForCustomerAsync();
                SelectedAccount = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Failed to delete account.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error deleting account: {ex.Message}";
        }
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingAccount = new Account();
        ErrorMessage = string.Empty;
    }

    private string GenerateAccountNumber()
    {
        return $"TK{DateTime.Now:yyyyMMddHHmmss}";
    }
}
