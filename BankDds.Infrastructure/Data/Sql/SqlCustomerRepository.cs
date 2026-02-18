using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of ICustomerRepository using ADO.NET.
/// Branch-scoped reads use the branch connection; cross-branch reads use the main bank connection.
/// All SqlExceptions are wrapped in InvalidOperationException with a user-friendly message.
/// </summary>
public class SqlCustomerRepository : ICustomerRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;
    private readonly ILogger<SqlCustomerRepository> _logger;

    public SqlCustomerRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        ILogger<SqlCustomerRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _logger = logger;
    }

    private string GetConnectionString() =>
        _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);

    public async Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
    {
        branchCode = branchCode.Trim();
        _logger.LogInformation("GetCustomersByBranch: branch={Branch}", branchCode);
        var customers = new List<Customer>();
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetCustomersByBranch", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@MACN", branchCode);
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) customers.Add(MapFromReader(reader));
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving customers for branch '{branchCode}': {ex.Message}", ex); }
        return customers;
    }

    public async Task<List<Customer>> GetAllCustomersAsync()
    {
        _logger.LogInformation("GetAllCustomers: cross-branch via Bank_Main");
        var customers = new List<Customer>();
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetBankConnection());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetAllCustomers", connection) { CommandType = CommandType.StoredProcedure };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) customers.Add(MapFromReader(reader));
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving all customers: {ex.Message}", ex); }
        return customers;
    }

    public async Task<Customer?> GetCustomerByCMNDAsync(string cmnd)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetCustomerByCMND", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CMND", cmnd);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync()) return MapFromReader(reader);
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving customer '{cmnd}': {ex.Message}", ex); }
        return null;
    }

    public async Task<bool> AddCustomerAsync(Customer customer)
    {
        try
        {
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(customer.MaCN));
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_AddCustomer", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CMND",     customer.CMND);
            command.Parameters.AddWithValue("@HO",       customer.Ho);
            command.Parameters.AddWithValue("@TEN",      customer.Ten);
            command.Parameters.AddWithValue("@NGAYSINH", (object?)customer.NgaySinh ?? DBNull.Value);
            command.Parameters.AddWithValue("@DIACHI",   (object?)customer.DiaChi   ?? DBNull.Value);
            command.Parameters.AddWithValue("@NGAYCAP",  (object?)customer.NgayCap  ?? DBNull.Value);
            command.Parameters.AddWithValue("@SDT",      (object?)customer.SDT      ?? DBNull.Value);
            command.Parameters.AddWithValue("@PHAI",     customer.Phai);
            command.Parameters.AddWithValue("@MACN",     customer.MaCN);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error adding customer: {ex.Message}", ex); }
    }

    public async Task<bool> UpdateCustomerAsync(Customer customer)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_UpdateCustomer", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CMND",     customer.CMND);
            command.Parameters.AddWithValue("@HO",       customer.Ho);
            command.Parameters.AddWithValue("@TEN",      customer.Ten);
            command.Parameters.AddWithValue("@NGAYSINH", (object?)customer.NgaySinh ?? DBNull.Value);
            command.Parameters.AddWithValue("@DIACHI",   (object?)customer.DiaChi   ?? DBNull.Value);
            command.Parameters.AddWithValue("@NGAYCAP",  (object?)customer.NgayCap  ?? DBNull.Value);
            command.Parameters.AddWithValue("@SDT",      (object?)customer.SDT      ?? DBNull.Value);
            command.Parameters.AddWithValue("@PHAI",     customer.Phai);
            command.Parameters.AddWithValue("@MACN",     customer.MaCN);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error updating customer: {ex.Message}", ex); }
    }

    public async Task<bool> DeleteCustomerAsync(string cmnd)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_DeleteCustomer", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CMND", cmnd);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error deleting customer: {ex.Message}", ex); }
    }

    public async Task<bool> RestoreCustomerAsync(string cmnd)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_RestoreCustomer", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@CMND", cmnd);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex) { throw new InvalidOperationException($"Database error restoring customer: {ex.Message}", ex); }
    }

    private static Customer MapFromReader(SqlDataReader reader) => new Customer
    {
        // nChar columns are space-padded to their fixed length in SQL Server;
        // Trim() normalises them so model comparisons work correctly.
        CMND        = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
        Ho          = reader.GetString(reader.GetOrdinal("HO")),
        Ten         = reader.GetString(reader.GetOrdinal("TEN")),
        NgaySinh    = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
        DiaChi      = reader.IsDBNull(reader.GetOrdinal("DIACHI"))   ? ""   : reader.GetString(reader.GetOrdinal("DIACHI")),
        NgayCap     = reader.IsDBNull(reader.GetOrdinal("NGAYCAP"))  ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
        SDT         = reader.IsDBNull(reader.GetOrdinal("SDT"))      ? ""   : reader.GetString(reader.GetOrdinal("SDT")),
        Phai        = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
        MaCN        = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
        TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangThaiXoa"))
    };
}
