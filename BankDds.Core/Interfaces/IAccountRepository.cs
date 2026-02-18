using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for Account data access operations
/// </summary>
public interface IAccountRepository
{
    Task<List<Account>> GetAccountsByBranchAsync(string branchCode);
    Task<List<Account>> GetAllAccountsAsync();
    Task<List<Account>> GetAccountsByCustomerAsync(string cmnd);
    Task<Account?> GetAccountAsync(string sotk);
    Task<bool> AddAccountAsync(Account account);
    Task<bool> UpdateAccountAsync(Account account);
    Task<bool> DeleteAccountAsync(string sotk);
    Task<bool> CloseAccountAsync(string sotk);
    Task<bool> ReopenAccountAsync(string sotk);
    
    /// <summary>
    /// Atomically transfers money between two accounts.
    /// Both account balances are updated in a single atomic operation.
    /// Returns false if accounts don't exist or insufficient balance.
    /// </summary>
    Task<bool> AtomicTransferAsync(string sotkFrom, string sotkTo, decimal amount);
}
