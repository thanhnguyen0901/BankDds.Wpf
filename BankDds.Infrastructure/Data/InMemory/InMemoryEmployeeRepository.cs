using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory implementation of IEmployeeRepository for development and testing
/// </summary>
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly List<Employee> _employees = new()
    {
        new Employee { MANV = "NV00000001", HO = "Nguyen", TEN = "Admin", DIACHI = "123 Admin St", CMND = "001111111", PHAI = "Nam", SDT = "0911111111", MACN = "BENTHANH", TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000002", HO = "Tran", TEN = "Manager", DIACHI = "456 Manager Ave", CMND = "002222222", PHAI = "Nam", SDT = "0922222222", MACN = "BENTHANH", TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000003", HO = "Le", TEN = "Teller", DIACHI = "789 Teller Rd", CMND = "003333333", PHAI = "Nu", SDT = "0933333333", MACN = "TANDINH", TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000004", HO = "Pham", TEN = "Staff", DIACHI = "321 Staff Blvd", CMND = "004444444", PHAI = "Nu", SDT = "0944444444", MACN = "TANDINH", TrangThaiXoa = 0 }
    };

    private int _nextIdCounter = 5;

    public Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode)
    {
        var employees = _employees.Where(e => e.MACN == branchCode).ToList();
        return Task.FromResult(employees);
    }

    public Task<List<Employee>> GetAllEmployeesAsync()
    {
        return Task.FromResult(_employees.ToList());
    }

    public Task<Employee?> GetEmployeeAsync(string manv)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        return Task.FromResult(employee);
    }

    public Task<bool> AddEmployeeAsync(Employee employee)
    {
        // Generate MANV if not provided (format: NV00000XXX - 10 characters)
        if (string.IsNullOrEmpty(employee.MANV))
        {
            employee.MANV = $"NV{_nextIdCounter:D8}";
            _nextIdCounter++;
        }
        
        _employees.Add(employee);
        return Task.FromResult(true);
    }

    public Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        var existing = _employees.FirstOrDefault(e => e.MANV == employee.MANV);
        if (existing == null)
            return Task.FromResult(false);

        existing.HO = employee.HO;
        existing.TEN = employee.TEN;
        existing.DIACHI = employee.DIACHI;
        existing.CMND = employee.CMND;
        existing.PHAI = employee.PHAI;
        existing.SDT = employee.SDT;
        existing.MACN = employee.MACN;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteEmployeeAsync(string manv)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        if (employee == null)
            return Task.FromResult(false);

        employee.TrangThaiXoa = 1; // Soft delete
        return Task.FromResult(true);
    }

    public Task<bool> RestoreEmployeeAsync(string manv)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        if (employee == null)
            return Task.FromResult(false);

        employee.TrangThaiXoa = 0; // Restore
        return Task.FromResult(true);
    }

    public Task<bool> TransferEmployeeAsync(string manv, string newBranch)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        if (employee == null)
            return Task.FromResult(false);

        employee.MACN = newBranch;
        return Task.FromResult(true);
    }
}
