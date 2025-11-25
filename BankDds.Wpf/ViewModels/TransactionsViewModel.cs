using Caliburn.Micro;
using BankDds.Core.Interfaces;

namespace BankDds.Wpf.ViewModels;

public class TransactionsViewModel : Screen
{
    private readonly ITransactionService _transactionService;
    private readonly IAccountService _accountService;

    public TransactionsViewModel(ITransactionService transactionService, IAccountService accountService)
    {
        _transactionService = transactionService;
        _accountService = accountService;
        DisplayName = "Transactions";
    }
}
