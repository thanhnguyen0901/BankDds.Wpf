using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode);
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<Employee?> GetEmployeeAsync(string manv);
        Task<bool> AddEmployeeAsync(Employee employee);
        Task<bool> UpdateEmployeeAsync(Employee employee);
        Task<bool> DeleteEmployeeAsync(string manv);
        Task<bool> RestoreEmployeeAsync(string manv);
        Task<bool> TransferEmployeeAsync(string manv, string newBranch);
        Task<string> GenerateEmployeeIdAsync();
        Task<bool> EmployeeExistsAsync(string manv);
    }
}