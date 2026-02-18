using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of IAccountRepository using ADO.NET transactions
/// </summary>
public class SqlAccountRepository : IAccountRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;

    public SqlAccountRepository(IConnectionStringProvider connectionStringProvider, IUserSession userSession)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
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
            using var connection = new SqlConnection(GetConnectionString());
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
            using var connection = new SqlConnection(GetConnectionString());
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
            using var connection = new SqlConnection(GetConnectionString());
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
            using var connection = new SqlConnection(GetConnectionString());
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

    /// <summary>
    /// Atomically transfers money between two accounts using a SQL transaction.
    /// Ensures ACID properties - either both accounts update or neither.
    /// </summary>
    public async Task<bool> AtomicTransferAsync(string sotkFrom, string sotkTo, decimal amount)
    {
        SqlConnection? connection = null;
        SqlTransaction? transaction = null;

        try
        {
            connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // BEGIN TRANSACTION
            transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // Step 1: Deduct from source account
            using (var command = new SqlCommand("SP_DeductFromAccount", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@SOTK", sotkFrom);
                command.Parameters.AddWithValue("@Amount", amount);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    // Source account not updated (not found or insufficient balance)
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            // Step 2: Add to destination account
            using (var command = new SqlCommand("SP_AddToAccount", connection, transaction))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@SOTK", sotkTo);
                command.Parameters.AddWithValue("@Amount", amount);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                {
                    // Destination account not updated (not found)
                    await transaction.RollbackAsync();
                    return false;
                }
            }

            // COMMIT TRANSACTION - Both operations successful
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            // ROLLBACK on any error
            if (transaction != null)
            {
                try
                {
                    await transaction.RollbackAsync();
                }
                catch
                {
                    // Rollback failed, but original exception is more important
                }
            }

            throw new InvalidOperationException($"Atomic transfer failed: {ex.Message}", ex);
        }
        finally
        {
            transaction?.Dispose();
            connection?.Dispose();
        }
    }

    private Account MapFromReader(SqlDataReader reader)
    {
        return new Account
        {
            SOTK = reader.GetString(reader.GetOrdinal("SOTK")),
            CMND = reader.GetString(reader.GetOrdinal("CMND")),
            SODU = reader.GetDecimal(reader.GetOrdinal("SODU")),
            MACN = reader.GetString(reader.GetOrdinal("MACN")),
            NGAYMOTK = reader.GetDateTime(reader.GetOrdinal("NGAYMOTK")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Active" : reader.GetString(reader.GetOrdinal("Status"))
        };
    }
}
