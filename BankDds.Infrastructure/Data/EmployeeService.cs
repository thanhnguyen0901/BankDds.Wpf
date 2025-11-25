using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class EmployeeService : IEmployeeService
{
    private readonly List<Employee> _employees = new()
    {
        new Employee { MANV = 1, HO = "Nguyen", TEN = "Admin", DIACHI = "123 Admin St", SDT = "0911111111", MACN = "BENTHANH" },
        new Employee { MANV = 2, HO = "Tran", TEN = "Manager", DIACHI = "456 Manager Ave", SDT = "0922222222", MACN = "BENTHANH" },
        new Employee { MANV = 3, HO = "Le", TEN = "Teller", DIACHI = "789 Teller Rd", SDT = "0933333333", MACN = "TANDINH" },
        new Employee { MANV = 4, HO = "Pham", TEN = "Staff", DIACHI = "321 Staff Blvd", SDT = "0944444444", MACN = "TANDINH" }
    };

    private int _nextId = 5;

    public Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode)
    {
        var employees = _employees.Where(e => e.MACN == branchCode).ToList();
        return Task.FromResult(employees);
    }

    public Task<List<Employee>> GetAllEmployeesAsync()
    {
        return Task.FromResult(_employees.ToList());
    }

    public Task<Employee?> GetEmployeeAsync(int manv)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        return Task.FromResult(employee);
    }

    public Task<bool> AddEmployeeAsync(Employee employee)
    {
        employee.MANV = _nextId++;
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
        existing.SDT = employee.SDT;
        existing.MACN = employee.MACN;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteEmployeeAsync(int manv)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        if (employee == null)
            return Task.FromResult(false);

        _employees.Remove(employee);
        return Task.FromResult(true);
    }

    public Task<bool> TransferEmployeeAsync(int manv, string newBranch)
    {
        var employee = _employees.FirstOrDefault(e => e.MANV == manv);
        if (employee == null)
            return Task.FromResult(false);

        employee.MACN = newBranch;
        return Task.FromResult(true);
    }
}
