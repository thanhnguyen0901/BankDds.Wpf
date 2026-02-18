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
        DisplayName = "Transactions";

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

    // CanExecute properties
    public bool CanDeposit => !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
                              decimal.TryParse(Amount, out var amt) && amt >= _minAmount;

    public bool CanWithdraw => !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
                               decimal.TryParse(Amount, out var amt) && amt >= _minAmount;

    public bool CanTransfer => !string.IsNullOrWhiteSpace(SelectedAccountNumber) &&
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
            MAGD     = "PRE_VALIDATE",   // placeholder – MAGD is generated by the service
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
            ErrorMessage = "Invalid amount format.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Minimum deposit amount is {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Maximum single transaction amount is {_maxSingleAmount:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Employee ID not found in session. Please log in again.";
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
                SuccessMessage = $"Successfully deposited {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Failed to process deposit. Check transaction history for details.";
            }
        });
    }

    public async Task Withdraw()
    {
        if (!CanWithdraw) return;

        if (!decimal.TryParse(Amount, out var amount))
        {
            ErrorMessage = "Invalid amount format.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Minimum withdrawal amount is {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Maximum single transaction amount is {_maxSingleAmount:N0} VND.";
            return;
        }

        // Check daily withdrawal limit
        var dailyTotal = await _transactionService.GetDailyWithdrawalTotalAsync(SelectedAccountNumber, DateTime.Today);
        if (dailyTotal + amount > _maxDailyWithdrawal)
        {
            ErrorMessage = $"Daily withdrawal limit exceeded. Today's total: {dailyTotal:N0} VND. " +
                          $"Limit: {_maxDailyWithdrawal:N0} VND. " +
                          $"Available: {_maxDailyWithdrawal - dailyTotal:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Employee ID not found in session. Please log in again.";
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
                SuccessMessage = $"Successfully withdrew {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Failed to process withdrawal. Check transaction history for details.";
            }
        });
    }

    public async Task Transfer()
    {
        if (!CanTransfer) return;

        if (!decimal.TryParse(Amount, out var amount))
        {
            ErrorMessage = "Invalid amount format.";
            return;
        }

        // Validate amount limits before transaction
        if (amount < _minAmount)
        {
            ErrorMessage = $"Minimum transfer amount is {_minAmount:N0} VND.";
            return;
        }

        if (amount > _maxSingleAmount)
        {
            ErrorMessage = $"Maximum single transaction amount is {_maxSingleAmount:N0} VND.";
            return;
        }

        if (SelectedAccountNumber == TransferToAccount)
        {
            ErrorMessage = "Cannot transfer to the same account.";
            return;
        }

        // Check daily transfer limit
        var dailyTotal = await _transactionService.GetDailyTransferTotalAsync(SelectedAccountNumber, DateTime.Today);
        if (dailyTotal + amount > _maxDailyTransfer)
        {
            ErrorMessage = $"Daily transfer limit exceeded. Today's total: {dailyTotal:N0} VND. " +
                          $"Limit: {_maxDailyTransfer:N0} VND. " +
                          $"Available: {_maxDailyTransfer - dailyTotal:N0} VND.";
            return;
        }

        // Get employee ID from session
        if (string.IsNullOrEmpty(_userSession.EmployeeId))
        {
            ErrorMessage = "Employee ID not found in session. Please log in again.";
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
                SuccessMessage = $"Transfer complete: {amount:N0} VND from {SelectedAccountNumber} → {TransferToAccount}.";
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
                ErrorMessage = "Transfer could not be completed. Please retry or check the transaction history.";
            }
        });
    }
}
