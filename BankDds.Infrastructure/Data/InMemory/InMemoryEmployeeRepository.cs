using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data.InMemory;

/// <summary>
/// In-memory implementation of IEmployeeRepository for development and testing.
/// MANV generation uses a static Interlocked counter initialised from seed data max,
/// guaranteeing uniqueness even under rapid concurrent Add calls (Option B).
/// Format: "NV" + 8 zero-padded decimal digits = nChar(10).
/// </summary>
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private readonly List<Employee> _employees = new()
    {
        new Employee { MANV = "NV00000001", HO = "Nguyen", TEN = "Admin",   DIACHI = "123 Admin St",    CMND = "0011111111", PHAI = "Nam", SDT = "0911111111", MACN = "BENTHANH", TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000002", HO = "Tran",   TEN = "Manager", DIACHI = "456 Manager Ave", CMND = "0022222222", PHAI = "Nam", SDT = "0922222222", MACN = "BENTHANH", TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000003", HO = "Le",     TEN = "Teller",  DIACHI = "789 Teller Rd",   CMND = "0033333333", PHAI = "Nữ", SDT = "0933333333", MACN = "TANDINH",  TrangThaiXoa = 0 },
        new Employee { MANV = "NV00000004", HO = "Pham",   TEN = "Staff",   DIACHI = "321 Staff Blvd",  CMND = "0044444444", PHAI = "Nữ", SDT = "0944444444", MACN = "TANDINH",  TrangThaiXoa = 0 }
    };

    // Static counter so the value survives DI re-resolution.
    // Initialized to 4 → first Interlocked.Increment returns 5, producing "NV00000005".
    // Must be static to remain monotonic across multiple instances (e.g. test scenarios).
    private static int _nextIdCounter = 4;

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
        if (_employees.Any(e => e.MANV == employee.MANV))
            return Task.FromResult(false); // duplicate MANV guard

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

    /// <summary>
    /// Thread-safe monotonic MANV generation.
    /// Interlocked.Increment guarantees uniqueness even under concurrent rapid Add calls.
    /// </summary>
    public Task<string> GenerateEmployeeIdAsync()
    {
        var id = Interlocked.Increment(ref _nextIdCounter);
        return Task.FromResult($"NV{id:D8}");
    }

    public Task<bool> EmployeeExistsAsync(string manv)
    {
        var exists = _employees.Any(e => e.MANV.Equals(manv, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(exists);
    }
}
