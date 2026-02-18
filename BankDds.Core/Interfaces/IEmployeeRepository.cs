using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

/// <summary>
/// Repository interface for Employee data access operations
/// </summary>
public interface IEmployeeRepository
{
    Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode);
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeAsync(string manv);
    Task<bool> AddEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(string manv);
    Task<bool> RestoreEmployeeAsync(string manv);
    Task<bool> TransferEmployeeAsync(string manv, string newBranch);
    /// <summary>
    /// Returns a collision-free MANV string in nChar(10) format ("NV" + 8 digits).
    /// InMemory: monotonic Interlocked counter. SQL: calls SP_GetNextManv scalar function.
    /// </summary>
    Task<string> GenerateEmployeeIdAsync();
    /// <summary>Returns true when an employee with <paramref name="manv"/> already exists (active or deleted).</summary>
    Task<bool> EmployeeExistsAsync(string manv);
}
