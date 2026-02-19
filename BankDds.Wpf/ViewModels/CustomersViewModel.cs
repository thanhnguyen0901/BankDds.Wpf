using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class CustomersViewModel : BaseViewModel
{
    private readonly ICustomerService _customerService;
    private readonly IAccountService _accountService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;
    private readonly CustomerValidator _validator;
    private readonly AccountValidator _accountValidator;
    
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private Customer _editingCustomer = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;
    
    // SubForm: Customer Accounts
    private ObservableCollection<Account> _customerAccounts = new();
    private Account? _selectedAccount;
    private Account _editingAccount = new();
    private bool _isEditingAccount;
    private string _accountErrorMessage = string.Empty;

    public CustomersViewModel(
        ICustomerService customerService,
        IAccountService accountService,
        IUserSession userSession,
        IDialogService dialogService,
        CustomerValidator validator,
        AccountValidator accountValidator)
    {
        _customerService = customerService;
        _accountService = accountService;
        _userSession = userSession;
        _dialogService = dialogService;
        _validator = validator;
        _accountValidator = accountValidator;
        DisplayName = "Customer Management";
    }

    #region Customer Properties

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
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => HasSelectedCustomer);
            NotifyOfPropertyChange(() => CanAddAccount);
            _ = LoadCustomerAccountsAsync();
        }
    }

    public Customer EditingCustomer
    {
        get => _editingCustomer;
        set
        {
            _editingCustomer = value;
            NotifyOfPropertyChange(() => EditingCustomer);
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
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => CanSave);
            NotifyOfPropertyChange(() => CanCancel);
        }
    }

    public new string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyOfPropertyChange(() => ErrorMessage);
            NotifyOfPropertyChange(() => HasError);
        }
    }

    public new bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelectedCustomer => SelectedCustomer != null;

    #endregion

    #region Account SubForm Properties

    public ObservableCollection<Account> CustomerAccounts
    {
        get => _customerAccounts;
        set
        {
            _customerAccounts = value;
            NotifyOfPropertyChange(() => CustomerAccounts);
            NotifyOfPropertyChange(() => HasAccounts);
        }
    }

    public Account? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            _selectedAccount = value;
            NotifyOfPropertyChange(() => SelectedAccount);
            NotifyOfPropertyChange(() => CanEditAccount);
            NotifyOfPropertyChange(() => CanCloseAccount);
            NotifyOfPropertyChange(() => CanReopenAccount);
        }
    }

    public Account EditingAccount
    {
        get => _editingAccount;
        set
        {
            _editingAccount = value;
            NotifyOfPropertyChange(() => EditingAccount);
            NotifyOfPropertyChange(() => CanSaveAccount);
        }
    }

    public bool IsEditingAccount
    {
        get => _isEditingAccount;
        set
        {
            _isEditingAccount = value;
            NotifyOfPropertyChange(() => IsEditingAccount);
            NotifyOfPropertyChange(() => CanAddAccount);
            NotifyOfPropertyChange(() => CanEditAccount);
            NotifyOfPropertyChange(() => CanSaveAccount);
            NotifyOfPropertyChange(() => CanCancelAccount);
        }
    }

    public string AccountErrorMessage
    {
        get => _accountErrorMessage;
        set
        {
            _accountErrorMessage = value;
            NotifyOfPropertyChange(() => AccountErrorMessage);
            NotifyOfPropertyChange(() => HasAccountError);
        }
    }

    public bool HasAccountError => !string.IsNullOrWhiteSpace(AccountErrorMessage);
    public bool HasAccounts => CustomerAccounts.Count > 0;

    #endregion

    #region Customer CanExecute Properties

    public bool CanAdd => !IsEditing;
    public bool CanEdit => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
    public bool CanDelete => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
    public bool CanRestore => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 1 && !IsEditing;
    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingCustomer.CMND);
    public bool CanCancel => IsEditing;

    #endregion

    #region Account CanExecute Properties

    public bool CanAddAccount => SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditingAccount;
    public bool CanEditAccount => SelectedAccount != null && !IsEditingAccount;
    public bool CanCloseAccount => SelectedAccount != null && SelectedAccount.Status == "Active" && !IsEditingAccount;
    public bool CanReopenAccount => SelectedAccount != null && SelectedAccount.Status == "Closed" && !IsEditingAccount;
    public bool CanSaveAccount => IsEditingAccount && !string.IsNullOrWhiteSpace(EditingAccount.SOTK);
    public bool CanCancelAccount => IsEditingAccount;

    #endregion

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadCustomersAsync();
    }

    #region Customer Methods

    private async Task LoadCustomersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
        {
            List<Customer> customers;
            
            if (_userSession.UserGroup == UserGroup.NganHang)
            {
                customers = await _customerService.GetAllCustomersAsync();
            }
            else
            {
                customers = await _customerService.GetCustomersByBranchAsync(_userSession.SelectedBranch);
            }

            Customers = new ObservableCollection<Customer>(customers);
        });
    }

    public void Add()
    {
        EditingCustomer = new Customer { MaCN = _userSession.SelectedBranch };
        SelectedCustomer = null;
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public void Edit()
    {
        if (SelectedCustomer == null) return;
        
        EditingCustomer = new Customer
        {
            CMND = SelectedCustomer.CMND,
            Ho = SelectedCustomer.Ho,
            Ten = SelectedCustomer.Ten,
            NgaySinh = SelectedCustomer.NgaySinh,
            DiaChi = SelectedCustomer.DiaChi,
            NgayCap = SelectedCustomer.NgayCap,
            SODT = SelectedCustomer.SODT,
            Phai = SelectedCustomer.Phai,
            MaCN = SelectedCustomer.MaCN
        };
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    public async Task Delete()
    {
        if (SelectedCustomer == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to delete customer '{SelectedCustomer.FullName}'?",
            "Delete Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _customerService.DeleteCustomerAsync(SelectedCustomer.CMND);
            if (result)
            {
                await LoadCustomersAsync();
                SelectedCustomer = null;
                SuccessMessage = "Customer deleted successfully.";
            }
            else
            {
                ErrorMessage = "Failed to delete customer";
            }
        });
    }

    public async Task Restore()
    {
        if (SelectedCustomer == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to restore customer '{SelectedCustomer.FullName}'?",
            "Restore Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _customerService.RestoreCustomerAsync(SelectedCustomer.CMND);
            if (result)
            {
                await LoadCustomersAsync();
                SuccessMessage = "Customer restored successfully.";
            }
            else
            {
                ErrorMessage = "Failed to restore customer";
            }
        });
    }

    public async Task Save()
    {
        var validationResult = await _validator.ValidateAsync(EditingCustomer);
        if (!validationResult.IsValid)
        {
            ErrorMessage = string.Join(Environment.NewLine, 
                validationResult.Errors.Select(e => e.ErrorMessage));
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            bool result;
            
            if (SelectedCustomer == null)
            {
                result = await _customerService.AddCustomerAsync(EditingCustomer);
            }
            else
            {
                result = await _customerService.UpdateCustomerAsync(EditingCustomer);
            }

            if (result)
            {
                IsEditing = false;
                await LoadCustomersAsync();
                SelectedCustomer = null;
                SuccessMessage = "Customer saved successfully.";
            }
            else
            {
                ErrorMessage = "Failed to save customer";
            }
        });
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingCustomer = new Customer();
        ErrorMessage = string.Empty;
    }

    #endregion

    #region Account SubForm Methods

    private async Task LoadCustomerAccountsAsync()
    {
        if (SelectedCustomer == null)
        {
            CustomerAccounts = new ObservableCollection<Account>();
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            var accounts = await _accountService.GetAccountsByCustomerAsync(SelectedCustomer.CMND);
            CustomerAccounts = new ObservableCollection<Account>(accounts);
            AccountErrorMessage = string.Empty;
        });
    }

    public void AddAccount()
    {
        if (SelectedCustomer == null) return;

        EditingAccount = new Account
        {
            CMND = SelectedCustomer.CMND,
            MACN = SelectedCustomer.MaCN,
            SODU = 0,
            NGAYMOTK = DateTime.Now,
            SOTK = GenerateAccountNumber(),
            Status = "Active"
        };
        IsEditingAccount = true;
        SelectedAccount = null;
        AccountErrorMessage = string.Empty;
    }

    public void EditAccount()
    {
        if (SelectedAccount == null) return;

        EditingAccount = new Account
        {
            SOTK = SelectedAccount.SOTK,
            CMND = SelectedAccount.CMND,
            SODU = SelectedAccount.SODU,
            MACN = SelectedAccount.MACN,
            NGAYMOTK = SelectedAccount.NGAYMOTK,
            Status = SelectedAccount.Status
        };
        IsEditingAccount = true;
        AccountErrorMessage = string.Empty;
    }

    public async Task SaveAccount()
    {
        var validationResult = await _accountValidator.ValidateAsync(EditingAccount);
        if (!validationResult.IsValid)
        {
            AccountErrorMessage = string.Join(Environment.NewLine, 
                validationResult.Errors.Select(e => e.ErrorMessage));
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
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
                IsEditingAccount = false;
                await LoadCustomerAccountsAsync();
                SelectedAccount = null;
                AccountErrorMessage = string.Empty;
                SuccessMessage = "Account saved successfully.";
            }
            else
            {
                AccountErrorMessage = "Failed to save account.";
            }
        });
    }

    public void CancelAccount()
    {
        IsEditingAccount = false;
        EditingAccount = new Account();
        AccountErrorMessage = string.Empty;
    }

    public async Task CloseAccount()
    {
        if (SelectedAccount == null) return;

        if (SelectedAccount.SODU != 0)
        {
            await _dialogService.ShowWarningAsync(
                "Cannot close account with non-zero balance.",
                "Close Account");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to close account '{SelectedAccount.SOTK}'?",
            "Close Account Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _accountService.CloseAccountAsync(SelectedAccount.SOTK);
            if (result)
            {
                await LoadCustomerAccountsAsync();
                SelectedAccount = null;
                SuccessMessage = "Account closed successfully.";
            }
            else
            {
                AccountErrorMessage = "Failed to close account.";
            }
        });
    }

    public async Task ReopenAccount()
    {
        if (SelectedAccount == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Are you sure you want to reopen account '{SelectedAccount.SOTK}'?",
            "Reopen Account Confirmation"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _accountService.ReopenAccountAsync(SelectedAccount.SOTK);
            if (result)
            {
                await LoadCustomerAccountsAsync();
                SelectedAccount = null;
                SuccessMessage = "Account reopened successfully.";
            }
            else
            {
                AccountErrorMessage = "Failed to reopen account.";
            }
        });
    }

    /// <summary>
    /// Generates a 9-character account number: "TK" + 7 zero-padded digits.
    /// Format matches nChar(9) SQL column and seed data (e.g. "TK0000001").
    /// </summary>
    private static string GenerateAccountNumber()
    {
        return $"TK{DateTime.Now.Ticks % 10_000_000:D7}";
    }

    #endregion
}
