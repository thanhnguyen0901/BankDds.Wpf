using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IAuthorizationService _authorizationService;

    public EmployeeService(IEmployeeRepository employeeRepository, IAuthorizationService authorizationService)
    {
        _employeeRepository = employeeRepository;
        _authorizationService = authorizationService;
    }

    public Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode)
    {
        _authorizationService.RequireCanAccessBranch(branchCode);
        return _employeeRepository.GetEmployeesByBranchAsync(branchCode);
    }

    public Task<List<Employee>> GetAllEmployeesAsync()
    {
        if (!_authorizationService.CanAccessBranch("ALL"))
        {
            throw new UnauthorizedAccessException("Only bank-level users can access all employees.");
        }
        return _employeeRepository.GetAllEmployeesAsync();
    }

    public async Task<Employee?> GetEmployeeAsync(string manv)
    {
        var employee = await _employeeRepository.GetEmployeeAsync(manv);
        if (employee == null)
            return null;

        _authorizationService.RequireCanAccessBranch(employee.MACN);
        return employee;
    }

    public async Task<bool> AddEmployeeAsync(Employee employee)
    {
        _authorizationService.RequireCanModifyBranch(employee.MACN);
        return await _employeeRepository.AddEmployeeAsync(employee);
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var existing = await _employeeRepository.GetEmployeeAsync(employee.MANV);
        if (existing == null)
            return false;

        _authorizationService.RequireCanModifyBranch(existing.MACN);
        _authorizationService.RequireCanModifyBranch(employee.MACN);
        return await _employeeRepository.UpdateEmployeeAsync(employee);
    }

    public async Task<bool> DeleteEmployeeAsync(string manv)
    {
        var employee = await _employeeRepository.GetEmployeeAsync(manv);
        if (employee == null)
            return false;

        _authorizationService.RequireCanModifyBranch(employee.MACN);
        return await _employeeRepository.DeleteEmployeeAsync(manv);
    }

    public async Task<bool> RestoreEmployeeAsync(string manv)
    {
        var employee = await _employeeRepository.GetEmployeeAsync(manv);
        if (employee == null)
            return false;

        _authorizationService.RequireCanModifyBranch(employee.MACN);
        return await _employeeRepository.RestoreEmployeeAsync(manv);
    }

    public async Task<bool> TransferEmployeeAsync(string manv, string newBranch)
    {
        var employee = await _employeeRepository.GetEmployeeAsync(manv);
        if (employee == null)
            return false;

        _authorizationService.RequireCanModifyBranch(employee.MACN);
        _authorizationService.RequireCanModifyBranch(newBranch);
        return await _employeeRepository.TransferEmployeeAsync(manv, newBranch);
    }

    /// <summary>
    /// Delegates to the repository for collision-free MANV generation.
    /// No auth check — this is a metadata operation, not a data read.
    /// </summary>
    public Task<string> GenerateEmployeeIdAsync() =>
        _employeeRepository.GenerateEmployeeIdAsync();

    /// <summary>
    /// Returns true when a MANV is already taken (active or soft-deleted).
    /// Used by the ViewModel to provide an early uniqueness error before hitting the DB.
    /// </summary>
    public Task<bool> EmployeeExistsAsync(string manv) =>
        _employeeRepository.EmployeeExistsAsync(manv);
}
