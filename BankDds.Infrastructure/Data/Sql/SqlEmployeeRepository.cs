using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
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

    public SqlEmployeeRepository(IConnectionStringProvider connectionStringProvider, IUserSession userSession)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
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
            command.Parameters.AddWithValue("@SDT",    (object?)employee.SDT    ?? DBNull.Value);
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
            command.Parameters.AddWithValue("@SDT",    (object?)employee.SDT    ?? DBNull.Value);
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
        MANV         = reader.GetString(reader.GetOrdinal("MANV")),
        HO           = reader.GetString(reader.GetOrdinal("HO")),
        TEN          = reader.GetString(reader.GetOrdinal("TEN")),
        DIACHI       = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
        CMND         = reader.IsDBNull(reader.GetOrdinal("CMND"))   ? "" : reader.GetString(reader.GetOrdinal("CMND")),
        PHAI         = reader.GetString(reader.GetOrdinal("PHAI")),
        SDT          = reader.IsDBNull(reader.GetOrdinal("SDT"))    ? "" : reader.GetString(reader.GetOrdinal("SDT")),
        MACN         = reader.GetString(reader.GetOrdinal("MACN")),
        TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangThaiXoa"))
    };
}
