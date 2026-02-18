using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class TransactionService : ITransactionService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthorizationService _authorizationService;

    public TransactionService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        IAuthorizationService authorizationService)
    {
        _transactionRepository = transactionRepository;
        _accountRepository = accountRepository;
        _authorizationService = authorizationService;
    }

    public async Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        _authorizationService.RequireCanAccessAccount(account.CMND);
        _authorizationService.RequireCanAccessBranch(account.MACN);
        
        return await _transactionRepository.GetTransactionsByAccountAsync(sotk);
    }

    public Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null)
    {
        _authorizationService.RequireCanAccessBranch(branchCode);
        return _transactionRepository.GetTransactionsByBranchAsync(branchCode, fromDate, toDate);
    }

    public async Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date)
    {
        var account = await _accountRepository.GetAccountAsync(accountNumber);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        _authorizationService.RequireCanAccessAccount(account.CMND);
        return await _transactionRepository.GetDailyWithdrawalTotalAsync(accountNumber, date);
    }

    public async Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date)
    {
        var account = await _accountRepository.GetAccountAsync(accountNumber);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        _authorizationService.RequireCanAccessAccount(account.CMND);
        return await _transactionRepository.GetDailyTransferTotalAsync(accountNumber, date);
    }

    public async Task<bool> DepositAsync(string sotk, decimal amount, string manv)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return false;

        _authorizationService.RequireCanPerformTransactions(account.MACN);
        
        return await _transactionRepository.DepositAsync(sotk, amount, manv);
    }

    public async Task<bool> WithdrawAsync(string sotk, decimal amount, string manv)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            return false;

        _authorizationService.RequireCanPerformTransactions(account.MACN);
        
        return await _transactionRepository.WithdrawAsync(sotk, amount, manv);
    }

    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
    {
        var accountFrom = await _accountRepository.GetAccountAsync(sotkFrom);
        if (accountFrom == null)
            throw new InvalidOperationException($"Source account '{sotkFrom}' not found.");

        var accountTo = await _accountRepository.GetAccountAsync(sotkTo);
        if (accountTo == null)
            throw new InvalidOperationException($"Destination account '{sotkTo}' not found.");

        _authorizationService.RequireCanPerformTransactions(accountFrom.MACN);

        if (accountFrom.MACN != accountTo.MACN)
        {
            // Cross-branch transfer: destination branch must be at least readable
            _authorizationService.RequireCanAccessBranch(accountTo.MACN);
        }

        return await _transactionRepository.TransferAsync(sotkFrom, sotkTo, amount, manv);
    }
}
