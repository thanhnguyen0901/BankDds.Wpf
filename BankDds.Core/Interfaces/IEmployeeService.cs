using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IEmployeeService
{
    Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode);
    Task<List<Employee>> GetAllEmployeesAsync();
    Task<Employee?> GetEmployeeAsync(int manv);
    Task<bool> AddEmployeeAsync(Employee employee);
    Task<bool> UpdateEmployeeAsync(Employee employee);
    Task<bool> DeleteEmployeeAsync(int manv);
    Task<bool> TransferEmployeeAsync(int manv, string newBranch);
}
