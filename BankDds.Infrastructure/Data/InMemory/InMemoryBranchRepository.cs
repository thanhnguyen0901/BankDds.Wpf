using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory branch repository seeded with the two initial branches (BENTHANH, TANDINH).
/// </summary>
public class InMemoryBranchRepository : IBranchRepository
{
    private readonly List<Branch> _branches = new()
    {
        new Branch
        {
            MACN   = "BENTHANH",
            TENCN  = "Bến Thành",
            DiaChi = "Số 1 Công trường Quách Thị Trang, Q.1, TP.HCM",
            SODT   = "02838292929"
        },
        new Branch
        {
            MACN   = "TANDINH",
            TENCN  = "Tân Định",
            DiaChi = "Số 50 Trần Quang Khải, Q.1, TP.HCM",
            SODT   = "02838441122"
        }
    };

    public Task<List<Branch>> GetAllBranchesAsync() =>
        Task.FromResult(_branches.ToList());

    public Task<Branch?> GetBranchAsync(string macn) =>
        Task.FromResult<Branch?>(
            _branches.FirstOrDefault(b => b.MACN.Equals(macn, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> AddBranchAsync(Branch branch)
    {
        if (_branches.Any(b => b.MACN.Equals(branch.MACN, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(false);
        _branches.Add(branch);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateBranchAsync(Branch branch)
    {
        var existing = _branches.FirstOrDefault(b =>
            b.MACN.Equals(branch.MACN, StringComparison.OrdinalIgnoreCase));
        if (existing == null) return Task.FromResult(false);
        existing.TENCN  = branch.TENCN;
        existing.DiaChi = branch.DiaChi;
        existing.SODT   = branch.SODT;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteBranchAsync(string macn)
    {
        var branch = _branches.FirstOrDefault(b =>
            b.MACN.Equals(macn, StringComparison.OrdinalIgnoreCase));
        if (branch == null) return Task.FromResult(false);
        _branches.Remove(branch);
        return Task.FromResult(true);
    }

    public Task<bool> BranchExistsAsync(string macn) =>
        Task.FromResult(_branches.Any(b =>
            b.MACN.Equals(macn, StringComparison.OrdinalIgnoreCase)));
}
