using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;
    private readonly IAuthorizationService _authorizationService;

    public BranchService(
        IBranchRepository branchRepository,
        IAuthorizationService authorizationService)
    {
        _branchRepository = branchRepository;
        _authorizationService = authorizationService;
    }

    // Read-only operations — all roles can query branches (needed for login dropdown, validators, etc.)
    public Task<List<Branch>> GetAllBranchesAsync() =>
        _branchRepository.GetAllBranchesAsync();

    public Task<Branch?> GetBranchAsync(string macn) =>
        _branchRepository.GetBranchAsync(macn);

    public Task<bool> BranchExistsAsync(string macn) =>
        _branchRepository.BranchExistsAsync(macn);

    // Write operations — NganHang only
    public Task<bool> AddBranchAsync(Branch branch)
    {
        if (!_authorizationService.CanAccessBranch("ALL"))
            throw new UnauthorizedAccessException("Only bank-level administrators can add branches.");
        return _branchRepository.AddBranchAsync(branch);
    }

    public Task<bool> UpdateBranchAsync(Branch branch)
    {
        if (!_authorizationService.CanAccessBranch("ALL"))
            throw new UnauthorizedAccessException("Only bank-level administrators can update branches.");
        return _branchRepository.UpdateBranchAsync(branch);
    }

    public Task<bool> DeleteBranchAsync(string macn)
    {
        if (!_authorizationService.CanAccessBranch("ALL"))
            throw new UnauthorizedAccessException("Only bank-level administrators can delete branches.");
        return _branchRepository.DeleteBranchAsync(macn);
    }
}
