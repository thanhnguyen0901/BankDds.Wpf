using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for Transaction data access operations
/// </summary>
public interface ITransactionRepository
{
    Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk);
    Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null);
    Task<bool> DepositAsync(string sotk, decimal amount, string manv);
    Task<bool> WithdrawAsync(string sotk, decimal amount, string manv);
    Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv);
    Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date);
    Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date);
}
