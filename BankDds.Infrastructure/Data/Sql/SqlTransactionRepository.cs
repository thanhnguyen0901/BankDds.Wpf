using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of ITransactionRepository using ADO.NET transactions
/// </summary>
public class SqlTransactionRepository : ITransactionRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;
    private readonly IAccountRepository _accountRepository;

    public SqlTransactionRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        IAccountRepository accountRepository)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _accountRepository = accountRepository;
    }

    private string GetConnectionString()
    {
        return _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);
    }

    public async Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk)
    {
        var transactions = new List<Transaction>();

        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetTransactionsByAccount", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(MapFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving transactions: {ex.Message}", ex);
        }

        return transactions;
    }

    public async Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var transactions = new List<Transaction>();

        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetTransactionsByBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@MACN", branchCode);
            command.Parameters.AddWithValue("@FromDate", fromDate ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@ToDate", toDate ?? (object)DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                transactions.Add(MapFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving transactions: {ex.Message}", ex);
        }

        return transactions;
    }

    public async Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetDailyWithdrawalTotal", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", accountNumber);
            command.Parameters.AddWithValue("@Date", date.Date);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error getting daily withdrawal total: {ex.Message}", ex);
        }
    }

    public async Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetDailyTransferTotal", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", accountNumber);
            command.Parameters.AddWithValue("@Date", date.Date);

            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error getting daily transfer total: {ex.Message}", ex);
        }
    }

    public async Task<bool> DepositAsync(string sotk, decimal amount, string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_Deposit", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@MANV", manv);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error performing deposit: {ex.Message}", ex);
        }
    }

    public async Task<bool> WithdrawAsync(string sotk, decimal amount, string manv)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_Withdraw", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@SOTK", sotk);
            command.Parameters.AddWithValue("@Amount", amount);
            command.Parameters.AddWithValue("@MANV", manv);

            var result = await command.ExecuteNonQueryAsync();
            return result > 0;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error performing withdrawal: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Atomically transfers money between two accounts using a single ADO.NET transaction.
    /// <para>
    /// Algorithm:
    /// <list type="number">
    ///   <item>Pre-validate amount &gt; 0 and accounts not identical (no DB round-trip).</item>
    ///   <item>Read source and destination accounts for rich error messages (outside tx).</item>
    ///   <item>BEGIN TRANSACTION (ReadCommitted).</item>
    ///   <item>SP_DeductFromAccount — deducts amount from source; the SP enforces its own
    ///       locked balance check (returns 0 rows if balance insufficient or account closed).</item>
    ///   <item>SP_AddToAccount — credits destination; the SP enforces account-active check.</item>
    ///   <item>SP_CreateTransferTransaction — records the audit log entry.</item>
    ///   <item>COMMIT — all three changes become visible atomically.</item>
    /// </list>
    /// Any failure (including concurrent modification detected by SP row counts) triggers
    /// ROLLBACK, leaving both account balances and the transaction log unchanged.
    /// </para>
    /// </summary>
    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
    {
        // ── Pre-validation (no DB round-trip) ────────────────────────────────
        if (amount <= 0)
            throw new InvalidOperationException("Transfer amount must be greater than 0.");

        if (sotkFrom == sotkTo)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        // ── Pre-validate accounts (outside the transaction — for rich error messages) ──
        var sourceAccount = await _accountRepository.GetAccountAsync(sotkFrom);
        if (sourceAccount == null)
            throw new InvalidOperationException($"Source account '{sotkFrom}' not found.");
        if (sourceAccount.Status == "Closed")
            throw new InvalidOperationException("Cannot transfer from a closed account.");
        if (sourceAccount.SODU < amount)
            throw new InvalidOperationException(
                $"Insufficient balance. Available: {sourceAccount.SODU:N0} VND, requested: {amount:N0} VND.");

        var destAccount = await _accountRepository.GetAccountAsync(sotkTo);
        if (destAccount == null)
            throw new InvalidOperationException($"Destination account '{sotkTo}' not found.");
        if (destAccount.Status == "Closed")
            throw new InvalidOperationException("Cannot transfer to a closed account.");

        // ── All writes in ONE connection + ONE transaction ────────────────────
        SqlConnection?  connection    = null;
        SqlTransaction? dbTransaction = null;

        try
        {
            connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // BEGIN TRANSACTION
            dbTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // Step 1: Deduct from source account.
            // SP enforces a locked balance check (WHERE SODU >= @Amount AND Status = 'Active'),
            // so 0 rows affected means a concurrent withdrawal changed the balance after our
            // pre-validation read above.
            using (var cmd = new SqlCommand("SP_DeductFromAccount", connection, dbTransaction)
                   { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@SOTK",   sotkFrom);
                cmd.Parameters.AddWithValue("@Amount", amount);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    throw new InvalidOperationException(
                        "Deduction from source account failed — balance may have changed concurrently.");
            }

            // Step 2: Credit destination account (same connection + transaction).
            using (var cmd = new SqlCommand("SP_AddToAccount", connection, dbTransaction)
                   { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@SOTK",   sotkTo);
                cmd.Parameters.AddWithValue("@Amount", amount);
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    throw new InvalidOperationException(
                        "Credit to destination account failed — account may have been modified concurrently.");
            }

            // Step 3: Record transaction log entry (same connection + transaction).
            // This entry is only committed if both balance updates succeed.
            using (var cmd = new SqlCommand("SP_CreateTransferTransaction", connection, dbTransaction)
                   { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@SOTK_FROM", sotkFrom);
                cmd.Parameters.AddWithValue("@SOTK_TO",   sotkTo);
                cmd.Parameters.AddWithValue("@Amount",    amount);
                cmd.Parameters.AddWithValue("@MANV",      manv);
                await cmd.ExecuteScalarAsync(); // returns MAGD; ignored here
            }

            // COMMIT — all three changes become visible atomically
            await dbTransaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            // ROLLBACK on any error — leaves both accounts and the log unchanged
            if (dbTransaction != null)
            {
                try { await dbTransaction.RollbackAsync(); }
                catch { /* ignore secondary rollback failures */ }
            }

            // Re-throw business-validation exceptions as-is so the caller gets the message
            if (ex is InvalidOperationException) throw;

            throw new InvalidOperationException($"Transfer failed: {ex.Message}", ex);
        }
        finally
        {
            dbTransaction?.Dispose();
            connection?.Dispose();
        }
    }

    private Transaction MapFromReader(SqlDataReader reader)
    {
        return new Transaction
        {
            MAGD = reader.GetString(reader.GetOrdinal("MAGD")),
            SOTK = reader.GetString(reader.GetOrdinal("SOTK")),
            LOAIGD = reader.GetString(reader.GetOrdinal("LOAIGD")),
            NGAYGD = reader.GetDateTime(reader.GetOrdinal("NGAYGD")),
            SOTIEN = reader.GetDecimal(reader.GetOrdinal("SOTIEN")),
            MANV = reader.GetString(reader.GetOrdinal("MANV")),
            SOTK_NHAN = reader.IsDBNull(reader.GetOrdinal("SOTK_NHAN")) ? null : reader.GetString(reader.GetOrdinal("SOTK_NHAN")),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Completed" : reader.GetString(reader.GetOrdinal("Status")),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage"))
        };
    }
}
