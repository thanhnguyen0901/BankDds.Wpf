using BankDds.Core.Formatting;
using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using System.Collections.ObjectModel;
using System.Linq;

namespace BankDds.Wpf.ViewModels
{
    /// <summary>
    /// Manages customer records and the branch-scoped account workflow hosted under the customer screen.
    /// </summary>
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
        private Customer? _lookupAccountCustomer;
        private Customer _editingCustomer = new();
        private bool _isEditing;
        private string _errorMessage = string.Empty;
        private ObservableCollection<Account> _customerAccounts = new();
        private Account? _selectedAccount;
        private Account _editingAccount = new();
        private bool _isEditingAccount;
        private string _accountErrorMessage = string.Empty;
        private string _accountCustomerLookupCmnd = string.Empty;

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
            DisplayName = "Quản lý khách hàng";
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
            set => SetSelectedCustomer(value, clearLookupContext: true);
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
                NotifyOfPropertyChange(() => CanLookupAccountCustomer);
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

        #region Account Properties
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
                NotifyOfPropertyChange(() => AccountOpenBranchDisplayName);
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
                NotifyOfPropertyChange(() => CanSaveAccount);
                NotifyOfPropertyChange(() => CanCancelAccount);
                NotifyOfPropertyChange(() => CanLookupAccountCustomer);
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

        public string AccountCustomerLookupCmnd
        {
            get => _accountCustomerLookupCmnd;
            set
            {
                _accountCustomerLookupCmnd = value;
                NotifyOfPropertyChange(() => AccountCustomerLookupCmnd);
                NotifyOfPropertyChange(() => CanLookupAccountCustomer);
            }
        }

        public Customer? CurrentAccountCustomer => _lookupAccountCustomer ?? _selectedCustomer;
        public bool HasAccountCustomer => CurrentAccountCustomer != null;
        public string AccountBranchDisplayName => DisplayText.Branch(_userSession.SelectedBranch);
        public string AccountOpenBranchDisplayName => EditingAccount.BranchDisplayName;

        public string AccountContextTitle => CurrentAccountCustomer == null
            ? "Tài khoản tại chi nhánh hiện tại"
            : $"Tài khoản — {CurrentAccountCustomer.FullName} (CMND: {CurrentAccountCustomer.CMND})";

        public string AccountContextSubtitle
        {
            get
            {
                if (CurrentAccountCustomer == null)
                {
                    return "Chọn khách hàng trong danh sách hoặc tra cứu CMND toàn hệ thống để mở tài khoản.";
                }

                return string.Equals(CurrentAccountCustomer.MaCN, _userSession.SelectedBranch, StringComparison.OrdinalIgnoreCase)
                    ? $"Khách thuộc chi nhánh {CurrentAccountCustomer.BranchDisplayName}. Tài khoản mới sẽ được mở tại {AccountBranchDisplayName}."
                    : $"Khách gốc thuộc chi nhánh {CurrentAccountCustomer.BranchDisplayName}. Tài khoản mới vẫn sẽ được mở tại {AccountBranchDisplayName}.";
            }
        }
        #endregion

        private bool CanModifyCustomerData => _userSession.UserGroup == UserGroup.ChiNhanh;

        #region Customer CanExecute Properties
        public bool CanAdd => CanModifyCustomerData && !IsEditing;
        public bool CanEdit => CanModifyCustomerData && SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
        public bool CanDelete => CanModifyCustomerData && SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 0 && !IsEditing;
        public bool CanRestore => CanModifyCustomerData && SelectedCustomer != null && SelectedCustomer.TrangThaiXoa == 1 && !IsEditing;
        public bool CanSave => IsEditing && !string.IsNullOrWhiteSpace(EditingCustomer.CMND);
        public bool CanCancel => IsEditing;
        #endregion

        #region Account CanExecute Properties
        public bool CanLookupAccountCustomer => CanModifyCustomerData && !IsEditing && !IsEditingAccount && !string.IsNullOrWhiteSpace(AccountCustomerLookupCmnd);
        public bool CanAddAccount => CanModifyCustomerData && CurrentAccountCustomer != null && CurrentAccountCustomer.TrangThaiXoa == 0 && !IsEditingAccount;
        public bool CanCloseAccount => CanModifyCustomerData && SelectedAccount != null && SelectedAccount.Status == "Active" && !IsEditingAccount;
        public bool CanReopenAccount => CanModifyCustomerData && SelectedAccount != null && SelectedAccount.Status == "Closed" && !IsEditingAccount;
        public bool CanSaveAccount => IsEditingAccount && CurrentAccountCustomer != null && !string.IsNullOrWhiteSpace(EditingAccount.SOTK);
        public bool CanCancelAccount => IsEditingAccount;
        #endregion

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await LoadCustomersAsync();
        }

        #region Customer Methods
        private void SetSelectedCustomer(Customer? customer, bool clearLookupContext)
        {
            _selectedCustomer = customer;

            if (clearLookupContext)
            {
                _lookupAccountCustomer = null;
                AccountCustomerLookupCmnd = customer?.CMND ?? string.Empty;
            }

            NotifyOfPropertyChange(() => SelectedCustomer);
            NotifyOfPropertyChange(() => CanEdit);
            NotifyOfPropertyChange(() => CanDelete);
            NotifyOfPropertyChange(() => CanRestore);
            NotifyOfPropertyChange(() => HasSelectedCustomer);
            NotifyOfPropertyChange(() => CurrentAccountCustomer);
            NotifyOfPropertyChange(() => HasAccountCustomer);
            NotifyOfPropertyChange(() => CanAddAccount);
            NotifyOfPropertyChange(() => AccountContextTitle);
            NotifyOfPropertyChange(() => AccountContextSubtitle);

            _ = LoadCustomerAccountsAsync();
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
            SetSelectedCustomer(null, clearLookupContext: true);
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
                $"Bạn có chắc muốn xóa khách hàng '{SelectedCustomer.FullName}'?",
                "Xác nhận xóa");
            if (!confirmed) return;

            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _customerService.DeleteCustomerAsync(SelectedCustomer.CMND);
                if (result)
                {
                    await LoadCustomersAsync();
                    SetSelectedCustomer(null, clearLookupContext: true);
                    SuccessMessage = "Xóa khách hàng thành công.";
                }
                else
                {
                    ErrorMessage = "Không thể xóa khách hàng.";
                }
            });
        }

        public async Task Restore()
        {
            if (SelectedCustomer == null) return;

            var confirmed = await _dialogService.ShowConfirmationAsync(
                $"Bạn có chắc muốn khôi phục khách hàng '{SelectedCustomer.FullName}'?",
                "Xác nhận khôi phục");
            if (!confirmed) return;

            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _customerService.RestoreCustomerAsync(SelectedCustomer.CMND);
                if (result)
                {
                    await LoadCustomersAsync();
                    SuccessMessage = "Khôi phục khách hàng thành công.";
                }
                else
                {
                    ErrorMessage = "Không thể khôi phục khách hàng.";
                }
            });
        }

        public async Task Save()
        {
            var validationResult = await _validator.ValidateAsync(EditingCustomer);
            if (!validationResult.IsValid)
            {
                ErrorMessage = string.Join(Environment.NewLine, validationResult.Errors.Select(error => error.ErrorMessage));
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
                    SetSelectedCustomer(null, clearLookupContext: true);
                    SuccessMessage = "Lưu khách hàng thành công.";
                }
                else
                {
                    ErrorMessage = "Không thể lưu khách hàng.";
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

        #region Account Methods
        private async Task LoadCustomerAccountsAsync()
        {
            var customer = CurrentAccountCustomer;
            if (customer == null)
            {
                CustomerAccounts = new ObservableCollection<Account>();
                SelectedAccount = null;
                return;
            }

            await ExecuteWithLoadingAsync(async () =>
            {
                var accounts = await _accountService.GetAccountsByCustomerAsync(customer.CMND);
                CustomerAccounts = new ObservableCollection<Account>(accounts);
                AccountErrorMessage = string.Empty;
            });
        }

        public async Task LookupAccountCustomer()
        {
            if (!CanLookupAccountCustomer) return;

            try
            {
                var customer = await _customerService.GetCustomerByCMNDAsync(AccountCustomerLookupCmnd.Trim());
                if (customer == null)
                {
                    AccountErrorMessage = $"Không tìm thấy khách hàng với CMND '{AccountCustomerLookupCmnd.Trim()}'.";
                    return;
                }

                _lookupAccountCustomer = customer;
                _selectedCustomer = null;
                NotifyOfPropertyChange(() => SelectedCustomer);
                NotifyOfPropertyChange(() => CanEdit);
                NotifyOfPropertyChange(() => CanDelete);
                NotifyOfPropertyChange(() => CanRestore);
                NotifyOfPropertyChange(() => HasSelectedCustomer);
                NotifyOfPropertyChange(() => CurrentAccountCustomer);
                NotifyOfPropertyChange(() => HasAccountCustomer);
                NotifyOfPropertyChange(() => CanAddAccount);
                NotifyOfPropertyChange(() => AccountContextTitle);
                NotifyOfPropertyChange(() => AccountContextSubtitle);
                SelectedAccount = null;
                AccountErrorMessage = string.Empty;
                await LoadCustomerAccountsAsync();
            }
            catch (Exception ex)
            {
                AccountErrorMessage = $"Không thể tra cứu khách hàng mở tài khoản: {ex.Message}";
            }
        }

        public void AddAccount()
        {
            var accountCustomer = CurrentAccountCustomer;
            if (accountCustomer == null) return;

            EditingAccount = new Account
            {
                CMND = accountCustomer.CMND,
                MACN = _userSession.SelectedBranch,
                SODU = 0,
                NGAYMOTK = DateTime.Now,
                SOTK = GenerateAccountNumber(),
                Status = "Active"
            };
            IsEditingAccount = true;
            SelectedAccount = null;
            AccountErrorMessage = string.Empty;
        }

        public async Task SaveAccount()
        {
            var accountCustomer = CurrentAccountCustomer;
            if (accountCustomer == null)
            {
                AccountErrorMessage = "Chưa chọn khách hàng để mở tài khoản.";
                return;
            }

            EditingAccount.CMND = accountCustomer.CMND;
            EditingAccount.MACN = _userSession.SelectedBranch;
            EditingAccount.SODU = 0;

            var validationResult = await _accountValidator.ValidateAsync(EditingAccount);
            if (!validationResult.IsValid)
            {
                AccountErrorMessage = string.Join(Environment.NewLine, validationResult.Errors.Select(error => error.ErrorMessage));
                return;
            }

            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _accountService.AddAccountAsync(EditingAccount);
                if (result)
                {
                    IsEditingAccount = false;
                    await LoadCustomerAccountsAsync();
                    SelectedAccount = null;
                    AccountErrorMessage = string.Empty;
                    SuccessMessage = "Mở tài khoản thành công.";
                }
                else
                {
                    AccountErrorMessage = "Không thể mở tài khoản.";
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
                    "Không thể đóng tài khoản có số dư khác 0.",
                    "Đóng tài khoản");
                return;
            }

            var confirmed = await _dialogService.ShowConfirmationAsync(
                $"Bạn có chắc muốn đóng tài khoản '{SelectedAccount.SOTK}'?",
                "Xác nhận đóng tài khoản");
            if (!confirmed) return;

            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _accountService.CloseAccountAsync(SelectedAccount.SOTK);
                if (result)
                {
                    await LoadCustomerAccountsAsync();
                    SelectedAccount = null;
                    SuccessMessage = "Đóng tài khoản thành công.";
                }
                else
                {
                    AccountErrorMessage = "Không thể đóng tài khoản.";
                }
            });
        }

        public async Task ReopenAccount()
        {
            if (SelectedAccount == null) return;

            var confirmed = await _dialogService.ShowConfirmationAsync(
                $"Bạn có chắc muốn mở lại tài khoản '{SelectedAccount.SOTK}'?",
                "Xác nhận mở lại tài khoản");
            if (!confirmed) return;

            await ExecuteWithLoadingAsync(async () =>
            {
                var result = await _accountService.ReopenAccountAsync(SelectedAccount.SOTK);
                if (result)
                {
                    await LoadCustomerAccountsAsync();
                    SelectedAccount = null;
                    SuccessMessage = "Mở lại tài khoản thành công.";
                }
                else
                {
                    AccountErrorMessage = "Không thể mở lại tài khoản.";
                }
            });
        }

        private static string GenerateAccountNumber()
        {
            return $"TK{DateTime.Now.Ticks % 10_000_000:D7}";
        }
        #endregion
    }
}
