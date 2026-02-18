using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IBranchService
{
    Task<List<Branch>> GetAllBranchesAsync();
    Task<Branch?> GetBranchAsync(string macn);
    Task<bool> AddBranchAsync(Branch branch);
    Task<bool> UpdateBranchAsync(Branch branch);
    Task<bool> DeleteBranchAsync(string macn);
    Task<bool> BranchExistsAsync(string macn);
}
