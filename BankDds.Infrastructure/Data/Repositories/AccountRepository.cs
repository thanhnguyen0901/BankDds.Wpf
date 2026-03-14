using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// SQL Server implementation of IAccountRepository using ADO.NET transactions
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;
    private readonly ILogger<AccountRepository> _logger;

    public AccountRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        ILogger<AccountRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);
    }

    public async Task<List<Account>> GetAccountsByBranchAsync(string branchCode)
    {
        var accounts = new List<Account>();

        try
        {
            // Route to the server that owns this branch's data, not the session branch.
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAccountsByBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN", branchCode);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(MapFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving accounts: {ex.Message}", ex);
        }

        return accounts;
    }

    public async Task<List<Account>> GetAllAccountsAsync()
    {
        var accounts = new List<Account>();

        try
        {
            // Publisher (NGANHANG) has all data via Merge Replication — no UNION ALL needed.
            using var connection = new SqlConnection(_connectionStringProvider.GetPublisherConnection());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAllAccounts", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(MapFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving accounts: {ex.Message}", ex);
        }

        return accounts;
    }

    public async Task<List<Account>> GetAccountsByCustomerAsync(string cmnd)
    {
        var accounts = new List<Account>();

        try
        {
            // Use Publisher so customer account lookup works across branches.
            using var connection = new SqlConnection(_connectionStringProvider.GetPublisherConnection());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAccountsByCustomer", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@CMND", cmnd);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(MapFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving accounts: {ex.Message}", ex);
        }

        return accounts;
    }

    public async Task<Account?> GetAccountAsync(string sotk)
    {
        try
        {
            // Use Publisher so account resolution is global (all branch fragments).
            using var connection = new SqlConnection(_connectionStringProvider.GetPublisherConnection());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapFromReader(reader);
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving account: {ex.Message}", ex);
        }

        return null;
    }

    public async Task<bool> AddAccountAsync(Account account)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_AddAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", account.SOTK);
            command.Parameters.AddWithValue("@CMND", account.CMND);
            command.Parameters.AddWithValue("@SODU", account.SODU);
            command.Parameters.AddWithValue("@MACN", account.MACN);
            command.Parameters.AddWithValue("@NGAYMOTK", account.NGAYMOTK);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error adding account: {ex.Message}", ex);
        }
    }

    public async Task<bool> UpdateAccountAsync(Account account)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_UpdateAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", account.SOTK);
            command.Parameters.AddWithValue("@SODU", account.SODU);
            command.Parameters.AddWithValue("@Status", account.Status);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error updating account: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteAccountAsync(string sotk)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_DeleteAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error deleting account: {ex.Message}", ex);
        }
    }

    public async Task<bool> CloseAccountAsync(string sotk)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_CloseAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error closing account: {ex.Message}", ex);
        }
    }

    public async Task<bool> ReopenAccountAsync(string sotk)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_ReopenAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error reopening account: {ex.Message}", ex);
        }
    }

    private Account MapFromReader(SqlDataReader reader)
    {
        return new Account
        {
            // nChar columns padded to fixed width — Trim() normalises for model comparisons.
            SOTK = reader.GetString(reader.GetOrdinal("SOTK")).Trim(),
            CMND = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
            SODU = reader.GetDecimal(reader.GetOrdinal("SODU")),
            MACN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
            NGAYMOTK = reader.GetDateTime(reader.GetOrdinal("NGAYMOTK")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Active" : reader.GetString(reader.GetOrdinal("Status")).Trim()
        };
    }
}

