using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;

using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class AccountsViewModel : BaseViewModel
{
    private readonly IAccountService _accountService;
    private readonly ICustomerService _customerService;
    private readonly IUserSession _userSession;
    private readonly IDialogService _dialogService;
    private readonly AccountValidator _validator;
    
    private ObservableCollection<Customer> _customers = new();
    private Customer? _selectedCustomer;
    private ObservableCollection<Account> _accounts = new();
    private Account? _selectedAccount;
    private Account _editingAccount = new();
    private bool _isEditing;
    private string _errorMessage = string.Empty;

    public AccountsViewModel(IAccountService accountService, ICustomerService customerService, IUserSession userSession, IDialogService dialogService, AccountValidator validator)
    {
        _accountService = accountService;
        _customerService = customerService;
        _userSession = userSession;
        _dialogService = dialogService;
        _validator = validator;
        DisplayName = "Quản lý tài khoản";
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
            NotifyOfPropertyChange(() => CanClose);
            NotifyOfPropertyChange(() => CanReopen);
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

    private bool CanModifyAccountData => _userSession.UserGroup == UserGroup.ChiNhanh;

    // CanExecute properties - Standard CRUD pattern
    public bool CanAdd => CanModifyAccountData && SelectedCustomer != null && !IsEditing;
    public bool CanEdit => CanModifyAccountData && SelectedAccount != null && !IsEditing;
    public bool CanDelete => CanModifyAccountData && SelectedAccount != null && !IsEditing;
    public bool CanClose => CanModifyAccountData && SelectedAccount != null && SelectedAccount.Status == "Active" && !IsEditing;
    public bool CanReopen => CanModifyAccountData && SelectedAccount != null && SelectedAccount.Status == "Closed" && !IsEditing;
    public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingAccount.SOTK) && SelectedCustomer != null;
    public bool CanCancel => IsEditing;

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        await LoadCustomersAsync();
    }

    private async Task LoadCustomersAsync()
    {
        await ExecuteWithLoadingAsync(async () =>
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
        });
    }

    private async Task LoadAccountsForCustomerAsync()
    {
        if (SelectedCustomer == null)
        {
            Accounts = new ObservableCollection<Account>();
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            var accounts = await _accountService.GetAccountsByCustomerAsync(SelectedCustomer.CMND);
            Accounts = new ObservableCollection<Account>(accounts);
        });
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
        // Validate before saving
        var validationResult = await _validator.ValidateAsync(EditingAccount);
        if (!validationResult.IsValid)
        {
            // Aggregate all validation errors
            ErrorMessage = string.Join(Environment.NewLine, 
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
                IsEditing = false;
                await LoadAccountsForCustomerAsync();
                SelectedAccount = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Không thể lưu tài khoản.";
            }
        });
    }

    public async Task Delete()
    {
        if (SelectedAccount == null) return;

        if (SelectedAccount.SODU != 0)
        {
            await _dialogService.ShowWarningAsync("Không thể xóa tài khoản có số dư khác 0.", "Xóa tài khoản");
            return;
        }

        // Show confirmation dialog
        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Bạn có chắc muốn xóa tài khoản '{SelectedAccount.SOTK}'?",
            "Xác nhận xóa"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
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
                ErrorMessage = "Không thể xóa tài khoản.";
            }
        });
    }

    public void Cancel()
    {
        IsEditing = false;
        EditingAccount = new Account();
        ErrorMessage = string.Empty;
    }

    public async Task Close()
    {
        if (SelectedAccount == null) return;

        if (SelectedAccount.SODU != 0)
        {
            await _dialogService.ShowWarningAsync("Không thể đóng tài khoản có số dư khác 0.", "Đóng tài khoản");
            return;
        }

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Bạn có chắc muốn đóng tài khoản '{SelectedAccount.SOTK}'?",
            "Xác nhận đóng tài khoản"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _accountService.CloseAccountAsync(SelectedAccount.SOTK);
            if (result)
            {
                await LoadAccountsForCustomerAsync();
                SelectedAccount = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Không thể đóng tài khoản.";
            }
        });
    }

    public async Task Reopen()
    {
        if (SelectedAccount == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            $"Bạn có chắc muốn mở lại tài khoản '{SelectedAccount.SOTK}'?",
            "Xác nhận mở lại tài khoản"
        );

        if (!confirmed) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _accountService.ReopenAccountAsync(SelectedAccount.SOTK);
            if (result)
            {
                await LoadAccountsForCustomerAsync();
                SelectedAccount = null;
                ErrorMessage = string.Empty;
            }
            else
            {
                ErrorMessage = "Không thể mở lại tài khoản.";
            }
        });
    }

    /// <summary>
    /// Generates a 9-character account number: "TK" + 7 zero-padded digits.
    /// Format matches nChar(9) SQL column and seed data (e.g. "TK0000001").
    /// Uses the last 7 significant digits of DateTime.Ticks to minimise collisions.
    /// </summary>
    private static string GenerateAccountNumber()
    {
        return $"TK{DateTime.Now.Ticks % 10_000_000:D7}";
    }
}

