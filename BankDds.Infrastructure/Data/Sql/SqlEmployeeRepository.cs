using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of IEmployeeRepository using ADO.NET.
/// Branch-scoped reads use the branch connection; cross-branch reads use the main bank connection.
/// All SqlExceptions are wrapped in InvalidOperationException with a user-friendly message.
/// </summary>
public class SqlEmployeeRepository : IEmployeeRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;
    private readonly ILogger<SqlEmployeeRepository> _logger;

    public SqlEmployeeRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        ILogger<SqlEmployeeRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _logger = logger;
    }

    private string GetConnectionString() =>
        _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);

    public async Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode)
    {
        var employees = new List<Employee>();
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetEmployeesByBranch", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MACN", branchCode);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) employees.Add(MapFromReader(reader));
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving employees for branch '{branchCode}': {ex.Message}", ex); }
        return employees;
    }

    public async Task<List<Employee>> GetAllEmployeesAsync()
    {
        var employees = new List<Employee>();
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetBankConnection());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetAllEmployees", connection) { CommandType = CommandType.StoredProcedure };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) employees.Add(MapFromReader(reader));
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving all employees: {ex.Message}", ex); }
        return employees;
    }

    public async Task<Employee?> GetEmployeeAsync(string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV", manv);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return MapFromReader(reader);
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving employee '{manv}': {ex.Message}", ex); }
        return null;
    }

    public async Task<bool> AddEmployeeAsync(Employee employee)
    {
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(employee.MACN));
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_AddEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV",   employee.MANV);
            command.Parameters.AddWithValue("@HO",     employee.HO);
            command.Parameters.AddWithValue("@TEN",    employee.TEN);
            command.Parameters.AddWithValue("@DIACHI", (object?)employee.DIACHI ?? DBNull.Value);
            command.Parameters.AddWithValue("@CMND",   (object?)employee.CMND   ?? DBNull.Value);
            command.Parameters.AddWithValue("@PHAI",   employee.PHAI);
            command.Parameters.AddWithValue("@SODT",    (object?)employee.SODT    ?? DBNull.Value);
            command.Parameters.AddWithValue("@MACN",   employee.MACN);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error adding employee: {ex.Message}", ex); }
    }

    public async Task<bool> UpdateEmployeeAsync(Employee employee)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_UpdateEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV",   employee.MANV);
            command.Parameters.AddWithValue("@HO",     employee.HO);
            command.Parameters.AddWithValue("@TEN",    employee.TEN);
            command.Parameters.AddWithValue("@DIACHI", (object?)employee.DIACHI ?? DBNull.Value);
            command.Parameters.AddWithValue("@CMND",   (object?)employee.CMND   ?? DBNull.Value);
            command.Parameters.AddWithValue("@PHAI",   employee.PHAI);
            command.Parameters.AddWithValue("@SODT",    (object?)employee.SODT    ?? DBNull.Value);
            command.Parameters.AddWithValue("@MACN",   employee.MACN);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error updating employee: {ex.Message}", ex); }
    }

    public async Task<bool> DeleteEmployeeAsync(string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_DeleteEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV", manv);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error deleting employee: {ex.Message}", ex); }
    }

    public async Task<bool> RestoreEmployeeAsync(string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_RestoreEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV", manv);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error restoring employee: {ex.Message}", ex); }
    }

    public async Task<bool> TransferEmployeeAsync(string manv, string newBranch)
    {
        try
        {
            // Cross-branch transfer: use the main bank connection
            using var connection = new SqlConnection(_connectionStringProvider.GetBankConnection());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_TransferEmployee", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV",     manv);
            command.Parameters.AddWithValue("@MACN_MOI", newBranch);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error transferring employee: {ex.Message}", ex); }
    }

    private static Employee MapFromReader(SqlDataReader reader) => new Employee
    {
        // nChar columns are space-padded — Trim() normalises for model comparisons.
        MANV         = reader.GetString(reader.GetOrdinal("MANV")).Trim(),
        HO           = reader.GetString(reader.GetOrdinal("HO")),
        TEN          = reader.GetString(reader.GetOrdinal("TEN")),
        DIACHI       = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
        CMND         = reader.IsDBNull(reader.GetOrdinal("CMND"))   ? "" : reader.GetString(reader.GetOrdinal("CMND")).Trim(),
        PHAI         = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
        SODT          = reader.IsDBNull(reader.GetOrdinal("SODT"))    ? "" : reader.GetString(reader.GetOrdinal("SODT")),
        MACN         = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
        TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangThaiXoa"))
    };

    /// <summary>
    /// Returns a collision-free MANV via SP_GetNextManv executed on the main bank server.
    /// SP contract (execute on Bank_Main, reads from linked servers or a central sequence):
    ///   SELECT 'NV' + RIGHT('00000000' + CAST(
    ///       ISNULL(MAX(CAST(SUBSTRING(MANV,3,8) AS INT)), 0) + 1
    ///   AS VARCHAR(8)), 8)
    ///   FROM NHANVIEN_ALL   -- a view unioning all branch NHANVIEN tables
    /// Alternatively use: SELECT NEXT VALUE FOR dbo.SEQ_MANV
    /// </summary>
    public async Task<string> GenerateEmployeeIdAsync()
    {
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetBankConnection());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetNextManv", connection) { CommandType = CommandType.StoredProcedure };
            var result = await command.ExecuteScalarAsync();
            return result?.ToString() ?? $"NV{DateTime.UtcNow.Ticks % 100_000_000:D8}";
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error generating employee ID: {ex.Message}", ex); }
    }

    /// <summary>
    /// SP contract: SP_EmployeeExists @MANV nchar(10) → SELECT COUNT(1) FROM NHANVIEN WHERE MANV = @MANV
    /// (executed on the current session branch connection)
    /// </summary>
    public async Task<bool> EmployeeExistsAsync(string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_EmployeeExists", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MANV", manv);
            var count = (int)(await command.ExecuteScalarAsync() ?? 0);
            return count > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error checking employee existence: {ex.Message}", ex); }
    }
}
