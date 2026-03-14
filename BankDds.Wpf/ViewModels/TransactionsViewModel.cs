using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Core.Validators;
using Microsoft.Extensions.Configuration;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class TransactionsViewModel : BaseViewModel
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly IUserSession _userSession;
    private readonly IConfiguration _configuration;
    private readonly TransactionValidator _validator;

    private string _selectedAccountNumber = string.Empty;
    private string _amount = string.Empty;
    private string _transferToAccount = string.Empty;
    private string _errorMessage = string.Empty;
    private ObservableCollection<Transaction> _recentTransactions = new();
    private int _selectedTabIndex = 0;

    // Transaction limits from configuration
    private readonly decimal _minAmount;
    private readonly decimal _maxSingleAmount;
    private readonly decimal _maxDailyWithdrawal;
    private readonly decimal _maxDailyTransfer;

    public TransactionsViewModel(
        ITransactionService transactionService,
        IAccountService accountService,
        IUserSession userSession,
        IConfiguration configuration,
        TransactionValidator validator)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _userSession = userSession;
        _configuration = configuration;
        _validator = validator;
        DisplayName = "Giao dịch";

        // Load transaction limits from configuration
        _minAmount = decimal.Parse(_configuration["TransactionLimits:MinTransactionAmount"] ?? "100000");
        _maxSingleAmount = decimal.Parse(_configuration["TransactionLimits:MaxSingleTransactionAmount"] ?? "50000000");
        _maxDailyWithdrawal = decimal.Parse(_configuration["TransactionLimits:MaxDailyWithdrawalAmount"] ?? "100000000");
        _maxDailyTransfer = decimal.Parse(_configuration["TransactionLimits:MaxDailyTransferAmount"] ?? "200000000");
    }

    public string SelectedAccountNumber
    {
        get => _selectedAccountNumber;
        set
        {
            _selectedAccountNumber = value;
            NotifyOfPropertyChange(() => SelectedAccountNumber);
            NotifyOfPropertyChange(() => CanDeposit);
            NotifyOfPropertyChange(() => CanWithdraw);
            NotifyOfPropertyChange(() => CanTransfer);
            _ = LoadTransactionsAsync();
        }
    }

    public string Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            NotifyOfPropertyChange(() => Amount);
            NotifyOfPropertyChange(() => CanDeposit);
            NotifyOfPropertyChange(() => CanWithdraw);
            NotifyOfPropertyChange(() => CanTransfer);
        }
    }

    public string TransferToAccount
    {
        get => _transferToAccount;
        set
        {
            _transferToAccount = value;
            NotifyOfPropertyChange(() => TransferToAccount);
            NotifyOfPropertyChange(() => CanTransfer);
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

    public ObservableCollection<Transaction> RecentTransactions
    {
        get => _recentTransactions;
        set
        {
            _recentTransactions = value;
            NotifyOfPropertyChange(() => RecentTransactions);
        }
    }

    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            _selectedTabIndex = value;
            NotifyOfPropertyChange(() => SelectedTabIndex);
        }
    }

    private bool CanPerformTransactions => _userSession.UserGroup == UserGroup.ChiNhanh;

    // CanExecute properties
    public bool CanDeposit => CanPerformTransactions &&
                              !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
                              decimal.TryParse(Amount, out var amt) && amt >= _minAmount;

    public bool CanWithdraw => CanPerformTransactions &&
                               !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
                               decimal.TryParse(Amount, out var amt) && amt >= _minAmount;

    public bool CanTransfer => CanPerformTransactions &&
                               !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
                               !string.IsNullOrWhiteSpace(TransferToAccount) &&
                               decimal.TryParse(Amount, out var amt) && amt >= _minAmount;

    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        return base.OnActivateAsync(cancellationToken);
    }

    /// <summary>
    /// Builds a temporary Transaction from the current UI inputs and runs it through
    /// <see cref="TransactionValidator"/> so field-level errors surface before the
    /// service layer is called.  Returns <c>true</c> when validation passes.
    /// </summary>
    private async Task<bool> ValidateTransactionInputAsync(
        string transactionType, decimal amount, string? receiverAccount = null)
    {
        var txn = new Transaction
        {
            MAGD     = 0,   // placeholder - MAGD is assigned by DB IDENTITY on insert
            SOTK     = SelectedAccountNumber,
            LOAIGD   = transactionType,
            SOTIEN   = amount,
            MANV     = _userSession.EmployeeId ?? string.Empty,
            NGAYGD   = DateTime.Now,
            SOTK_NHAN = receiverAccount
        };

        var result = await _validator.ValidateAsync(txn);
        if (!result.IsValid)
        {
            ErrorMessage = string.Join(Environment.NewLine,
                result.Errors.Select(e => e.ErrorMessage));
            return false;
        }
        return true;
    }

    private async Task LoadTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedAccountNumber))
        {
            RecentTransactions = new ObservableCollection<Transaction>();
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            var transactions = await _transactionService.GetTransactionsByAccountAsync(SelectedAccountNumber);
            RecentTransactions = new ObservableCollection<Transaction>(transactions.OrderByDescending(t => t.NGAYGD).Take(20));
        });
    }

    public async Task Deposit()
    {
        if (!CanDeposit) return;

        if (!decimal.TryParse(Amount, out var amount))
        {
            ErrorMessage = "Định dạng số tiền không hợp lệ.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Số tiền gửi tối thiểu là {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Số tiền tối đa cho một giao dịch là {_maxSingleAmount:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Không tìm thấy mã nhân viên trong phiên đăng nhập. Vui lòng đăng nhập lại.";
            return;
        }

        // FluentValidation check on user-provided fields
        if (!await ValidateTransactionInputAsync("GT", amount))
            return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _transactionService.DepositAsync(SelectedAccountNumber, amount, _userSession.EmployeeId);

            if (result)
            {
                SuccessMessage = $"Gửi tiền thành công: {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Không thể xử lý giao dịch gửi tiền. Vui lòng kiểm tra lịch sử giao dịch.";
            }
        });
    }

    public async Task Withdraw()
    {
        if (!CanWithdraw) return;

        if (!decimal.TryParse(Amount, out var amount))
        {
            ErrorMessage = "Định dạng số tiền không hợp lệ.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Số tiền rút tối thiểu là {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Số tiền tối đa cho một giao dịch là {_maxSingleAmount:N0} VND.";
            return;
        }

        // Check daily withdrawal limit
        var dailyTotal = await _transactionService.GetDailyWithdrawalTotalAsync(SelectedAccountNumber, DateTime.Today);
        if (dailyTotal + amount > _maxDailyWithdrawal)
        {
            ErrorMessage = $"Vượt hạn mức rút tiền trong ngày. Tổng hôm nay: {dailyTotal:N0} VND. " +
                          $"Hạn mức: {_maxDailyWithdrawal:N0} VND. " +
                          $"Còn lại có thể rút: {_maxDailyWithdrawal - dailyTotal:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Không tìm thấy mã nhân viên trong phiên đăng nhập. Vui lòng đăng nhập lại.";
            return;
        }

        // FluentValidation check on user-provided fields
        if (!await ValidateTransactionInputAsync("RT", amount))
            return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _transactionService.WithdrawAsync(SelectedAccountNumber, amount, _userSession.EmployeeId);

            if (result)
            {
                SuccessMessage = $"Rút tiền thành công: {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Không thể xử lý giao dịch rút tiền. Vui lòng kiểm tra lịch sử giao dịch.";
            }
        });
    }

    public async Task Transfer()
    {
        if (!CanTransfer) return;

        if (!decimal.TryParse(Amount, out var amount))
        {
            ErrorMessage = "Định dạng số tiền không hợp lệ.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Số tiền chuyển tối thiểu là {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Số tiền tối đa cho một giao dịch là {_maxSingleAmount:N0} VND.";
            return;
        }

        if (SelectedAccountNumber == TransferToAccount)
        {
            ErrorMessage = "Không thể chuyển đến cùng một tài khoản.";
            return;
        }

        // Check daily transfer limit
        var dailyTotal = await _transactionService.GetDailyTransferTotalAsync(SelectedAccountNumber, DateTime.Today);
        if (dailyTotal + amount > _maxDailyTransfer)
        {
            ErrorMessage = $"Vượt hạn mức chuyển tiền trong ngày. Tổng hôm nay: {dailyTotal:N0} VND. " +
                          $"Hạn mức: {_maxDailyTransfer:N0} VND. " +
                          $"Còn lại có thể chuyển: {_maxDailyTransfer - dailyTotal:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Không tìm thấy mã nhân viên trong phiên đăng nhập. Vui lòng đăng nhập lại.";
            return;
        }

        // FluentValidation check on user-provided fields
        if (!await ValidateTransactionInputAsync("CT", amount, TransferToAccount))
            return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await _transactionService.TransferAsync(SelectedAccountNumber, TransferToAccount, amount, _userSession.EmployeeId);

            if (result)
            {
                SuccessMessage = $"Chuyển tiền thành công: {amount:N0} VND từ {SelectedAccountNumber} sang {TransferToAccount}.";
                Amount = string.Empty;
                TransferToAccount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                // Reached only if the repository returns false without throwing.
                // Specific business failures (insufficient balance, closed account, etc.)
                // throw InvalidOperationException, which BaseViewModel.ExecuteWithLoadingAsync
                // catches and displays directly as ErrorMessage.
                ErrorMessage = "Không thể hoàn tất chuyển tiền. Vui lòng thử lại hoặc kiểm tra lịch sử giao dịch.";
            }
        });
    }
}

