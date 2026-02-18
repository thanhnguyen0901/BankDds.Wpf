using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IBranchRepository
{
    Task<List<Branch>> GetAllBranchesAsync();
    Task<Branch?> GetBranchAsync(string macn);
    Task<bool> AddBranchAsync(Branch branch);
    Task<bool> UpdateBranchAsync(Branch branch);
    Task<bool> DeleteBranchAsync(string macn);
    /// <summary>
    /// Returns true when <paramref name="macn"/> matches an existing branch code.
    /// Used by validators — no authorisation required.
    /// </summary>
    Task<bool> BranchExistsAsync(string macn);
}
