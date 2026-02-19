using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of IBranchRepository.
/// All operations execute against Bank_Main (SERVER3) because CHINHANH is a central
/// reference table, not stored on individual branch servers.
///
/// Required stored procedures on Bank_Main:
///
///   SP_GetBranches
///     Returns: MACN nChar(10), TENCN nvarchar(50), DIACHI nvarchar(100), SODT varchar(15)
///
///   SP_GetBranch  @MACN nChar(10)
///     Returns: same columns (single row or empty set)
///
///   SP_AddBranch  @MACN nChar(10), @TENCN nvarchar(50), @DIACHI nvarchar(100) = NULL, @SODT varchar(15) = NULL
///     Returns: rows affected (1 = success, 0 = duplicate MACN)
///
///   SP_UpdateBranch  @MACN nChar(10), @TENCN nvarchar(50), @DIACHI nvarchar(100) = NULL, @SODT varchar(15) = NULL
///     Returns: rows affected
///
///   SP_DeleteBranch  @MACN nChar(10)
///     Returns: rows affected (0 if branch has linked accounts/customers)
/// </summary>
public class SqlBranchRepository : IBranchRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly ILogger<SqlBranchRepository> _logger;

    public SqlBranchRepository(
        IConnectionStringProvider connectionStringProvider,
        ILogger<SqlBranchRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _logger = logger;
    }

    // All branch CRUD runs on Bank_Main — branches are a central reference table.
    private SqlConnection CreateBankConnection() =>
        new SqlConnection(_connectionStringProvider.GetBankConnection());

    public async Task<List<Branch>> GetAllBranchesAsync()
    {
        var branches = new List<Branch>();
        try
        {
            using var connection = CreateBankConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetBranches", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                branches.Add(MapFromReader(reader));
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving branches: {ex.Message}", ex);
        }
        return branches;
    }

    public async Task<Branch?> GetBranchAsync(string macn)
    {
        try
        {
            using var connection = CreateBankConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_GetBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN", macn);
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapFromReader(reader);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving branch: {ex.Message}", ex);
        }
        return null;
    }

    public async Task<bool> AddBranchAsync(Branch branch)
    {
        try
        {
            using var connection = CreateBankConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_AddBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN",   branch.MACN);
            command.Parameters.AddWithValue("@TENCN",  branch.TENCN);
            command.Parameters.AddWithValue("@DIACHI", string.IsNullOrEmpty(branch.DiaChi) ? (object)DBNull.Value : branch.DiaChi);
            command.Parameters.AddWithValue("@SODT",   string.IsNullOrEmpty(branch.SODT)   ? (object)DBNull.Value : branch.SODT);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error adding branch: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateBranchAsync(Branch branch)
    {
        try
        {
            using var connection = CreateBankConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_UpdateBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN",   branch.MACN);
            command.Parameters.AddWithValue("@TENCN",  branch.TENCN);
            command.Parameters.AddWithValue("@DIACHI", string.IsNullOrEmpty(branch.DiaChi) ? (object)DBNull.Value : branch.DiaChi);
            command.Parameters.AddWithValue("@SODT",   string.IsNullOrEmpty(branch.SODT)   ? (object)DBNull.Value : branch.SODT);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error updating branch: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteBranchAsync(string macn)
    {
        try
        {
            using var connection = CreateBankConnection();
            await connection.OpenAsync();
            using var command = new SqlCommand("SP_DeleteBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN", macn);
            return await command.ExecuteNonQueryAsync() > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error deleting branch: {ex.Message}", ex);
        }
    }

    public async Task<bool> BranchExistsAsync(string macn)
    {
        var branch = await GetBranchAsync(macn);
        return branch != null;
    }

    private static Branch MapFromReader(SqlDataReader reader) => new()
    {
        // MACN is nChar(10) — already trimmed.
        MACN   = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
        TENCN  = reader.GetString(reader.GetOrdinal("TENCN")),
        DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? string.Empty : reader.GetString(reader.GetOrdinal("DIACHI")),
        SODT   = reader.IsDBNull(reader.GetOrdinal("SODT"))   ? string.Empty : reader.GetString(reader.GetOrdinal("SODT"))
    };
}
