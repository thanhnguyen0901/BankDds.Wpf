using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class TransactionsViewModel : Screen
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;
    private readonly IUserSession _userSession;

    private string _selectedAccountNumber = string.Empty;
    private string _amount = string.Empty;
    private string _transferToAccount = string.Empty;
    private string _errorMessage = string.Empty;
    private ObservableCollection<Transaction> _recentTransactions = new();
    private int _selectedTabIndex = 0;

    public TransactionsViewModel(ITransactionService transactionService, IAccountService accountService, IUserSession userSession)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        _userSession = userSession;
        DisplayName = "Transactions";
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
                              decimal.TryParse(Amount, out var amt) && amt >= 100000;
    
    public bool CanWithdraw => !string.IsNullOrWhiteSpace(SelectedAccountNumber) && 
                               decimal.TryParse(Amount, out var amt) && amt >= 100000;
    
    public bool CanTransfer => !string.IsNullOrWhiteSpace(SelectedAccountNumber) && 
                               !string.IsNullOrWhiteSpace(TransferToAccount) &&
                               decimal.TryParse(Amount, out var amt) && amt > 0;

    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        return base.OnActivateAsync(cancellationToken);
    }

    private async Task LoadTransactionsAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedAccountNumber))
        {
            RecentTransactions = new ObservableCollection<Transaction>();
            return;
        }

        try
        {
            var transactions = await _transactionService.GetTransactionsByAccountAsync(SelectedAccountNumber);
            RecentTransactions = new ObservableCollection<Transaction>(transactions.OrderByDescending(t => t.NGAYGD).Take(20));
            ErrorMessage = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading transactions: {ex.Message}";
        }
    }

    public async Task Deposit()
    {
        if (!CanDeposit) return;

        try
        {
            if (!decimal.TryParse(Amount, out var amount))
            {
                ErrorMessage = "Invalid amount format.";
                return;
            }

            if (amount < 100000)
            {
                ErrorMessage = "Minimum deposit amount is 100,000 VND.";
                return;
            }

            // Get employee ID from session (in real app, this would be the logged-in employee)
            int employeeId = 1; // Default to admin for now

            var result = await _transactionService.DepositAsync(SelectedAccountNumber, amount, employeeId);
            
            if (result)
            {
                ErrorMessage = $"Successfully deposited {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Failed to process deposit.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing deposit: {ex.Message}";
        }
    }

    public async Task Withdraw()
    {
        if (!CanWithdraw) return;

        try
        {
            if (!decimal.TryParse(Amount, out var amount))
            {
                ErrorMessage = "Invalid amount format.";
                return;
            }

            if (amount < 100000)
            {
                ErrorMessage = "Minimum withdrawal amount is 100,000 VND.";
                return;
            }

            // Get employee ID from session
            int employeeId = 1;

            var result = await _transactionService.WithdrawAsync(SelectedAccountNumber, amount, employeeId);
            
            if (result)
            {
                ErrorMessage = $"Successfully withdrew {amount:N0} VND.";
                Amount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Failed to process withdrawal. Check account balance.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing withdrawal: {ex.Message}";
        }
    }

    public async Task Transfer()
    {
        if (!CanTransfer) return;

        try
        {
            if (!decimal.TryParse(Amount, out var amount))
            {
                ErrorMessage = "Invalid amount format.";
                return;
            }

            if (amount <= 0)
            {
                ErrorMessage = "Transfer amount must be greater than 0.";
                return;
            }

            if (SelectedAccountNumber == TransferToAccount)
            {
                ErrorMessage = "Cannot transfer to the same account.";
                return;
            }

            // Get employee ID from session
            int employeeId = 1;

            var result = await _transactionService.TransferAsync(SelectedAccountNumber, TransferToAccount, amount, employeeId);
            
            if (result)
            {
                ErrorMessage = $"Successfully transferred {amount:N0} VND to {TransferToAccount}.";
                Amount = string.Empty;
                TransferToAccount = string.Empty;
                await LoadTransactionsAsync();
            }
            else
            {
                ErrorMessage = "Failed to process transfer. Check account balances and account numbers.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error processing transfer: {ex.Message}";
        }
    }
}
