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
}
