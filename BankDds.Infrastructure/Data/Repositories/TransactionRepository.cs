using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// SQL Server implementation of ITransactionRepository using ADO.NET transactions
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;
    private readonly ILogger<TransactionRepository> _logger;

    public TransactionRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        ILogger<TransactionRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _logger = logger;
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
            // GAP-07: route to the server that owns this branch's data, not the session branch.
            // The caller always supplies a specific branchCode here, so no ALL guard needed.
            using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
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
        sotk = sotk.Trim(); manv = manv.Trim();
        _logger.LogInformation("Deposit: SOTK={SOTK} Amount={Amount} MANV={MANV}", sotk, amount, manv);
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
        sotk = sotk.Trim(); manv = manv.Trim();
        _logger.LogInformation("Withdraw: SOTK={SOTK} Amount={Amount} MANV={MANV}", sotk, amount, manv);
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
    /// Unified transfer — delegates 100% to SP_CrossBranchTransfer on the
    /// source-branch subscriber.  The SP handles both same-branch (local TXN)
    /// and cross-branch (DISTRIBUTED TRANSACTION via LINK1) paths internally.
    /// <para>
    /// Banking rule: C# calls ONE SP on the source branch; no pre-validation of
    /// the destination account that would incorrectly block remote accounts.
    /// </para>
    /// </summary>
    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
    {
        sotkFrom = sotkFrom.Trim(); sotkTo = sotkTo.Trim(); manv = manv.Trim();
        _logger.LogInformation("Transfer: From={SotkFrom} To={SotkTo} Amount={Amount} MANV={MANV}",
                               sotkFrom, sotkTo, amount, manv);

        // ── Minimal pre-validation (no DB round-trip) ────────────────────────
        if (amount <= 0)
            throw new InvalidOperationException("Transfer amount must be greater than 0.");

        if (sotkFrom == sotkTo)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        // ── Single SP call on source branch ──────────────────────────────────
        // SP_CrossBranchTransfer validates source & destination, detects
        // same-branch vs cross-branch, and executes the appropriate transaction
        // (local or distributed via LINK1).  All error codes are returned as
        // RAISERROR/THROW which surface as SqlException in C#.
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var cmd = new SqlCommand("SP_CrossBranchTransfer", connection)
            {
                CommandType    = CommandType.StoredProcedure,
                // Distributed transactions via MSDTC can take longer than local
                CommandTimeout = 60
            };
            cmd.Parameters.AddWithValue("@SOTK_CHUYEN", sotkFrom);
            cmd.Parameters.AddWithValue("@SOTK_NHAN",   sotkTo);
            cmd.Parameters.AddWithValue("@SOTIEN",      amount);
            cmd.Parameters.AddWithValue("@MANV",        manv);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex) when (ex.Number == 2812)
        {
            // 2812: stored procedure not found
            _logger.LogError(ex, "SP_CrossBranchTransfer not found on branch {Branch}",
                             _userSession.SelectedBranch);
            throw new InvalidOperationException(
                $"SP_CrossBranchTransfer not found on branch '{_userSession.SelectedBranch}'. " +
                "Ensure the SP has been replicated from the Publisher.",
                ex);
        }
        catch (SqlException ex) when (ex.Number is 8501 or 8517 or 7391)
        {
            // 8501/8517: MSDTC unavailable; 7391: distributed TX not supported
            _logger.LogError(ex, "MSDTC error during cross-branch transfer");
            throw new InvalidOperationException(
                "MSDTC is not available for cross-branch transfer. " +
                "Ensure the Distributed Transaction Coordinator service is running on both servers.",
                ex);
        }
        catch (SqlException ex) when (ex.Number is 7202 or 7399 or 7312)
        {
            // 7202: linked server not found; 7399: provider error; 7312: access denied
            _logger.LogError(ex, "Linked Server (LINK1) error during cross-branch transfer");
            throw new InvalidOperationException(
                "Linked Server LINK1 is not configured. " +
                "Configure Linked Server LINK1 via SSMS UI runbook (docs/sql/SETUP_SSMS_UI_FIRST_RUNBOOK.md) or refer to sql/archive/06_linked_servers.sql (legacy).",
                ex);
        }
        catch (SqlException ex) when (ex.Number >= 50000)
        {
            // SP user-defined errors (RAISERROR / THROW 50001–50007)
            _logger.LogWarning(ex, "Transfer business rule violation: {Message}", ex.Message);
            throw new InvalidOperationException(ex.Message, ex);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Transfer SQL error {Number}: {Message}", ex.Number, ex.Message);
            throw new InvalidOperationException(
                $"Transfer failed (SQL error {ex.Number}): {ex.Message}", ex);
        }
    }

    private Transaction MapFromReader(SqlDataReader reader)
    {
        return new Transaction
        {
            // nChar columns space-padded — Trim() normalises for model comparisons.
            MAGD = reader.GetInt32(reader.GetOrdinal("MAGD")),
            SOTK = reader.GetString(reader.GetOrdinal("SOTK")).Trim(),
            LOAIGD = reader.GetString(reader.GetOrdinal("LOAIGD")).Trim(),
            NGAYGD = reader.GetDateTime(reader.GetOrdinal("NGAYGD")),
            SOTIEN = reader.GetDecimal(reader.GetOrdinal("SOTIEN")),
            MANV = reader.GetString(reader.GetOrdinal("MANV")).Trim(),
            SOTK_NHAN = reader.IsDBNull(reader.GetOrdinal("SOTK_NHAN")) ? null : reader.GetString(reader.GetOrdinal("SOTK_NHAN")).Trim(),
            Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Completed" : reader.GetString(reader.GetOrdinal("Status")).Trim(),
            ErrorMessage = reader.IsDBNull(reader.GetOrdinal("ErrorMessage")) ? null : reader.GetString(reader.GetOrdinal("ErrorMessage"))
        };
    }
}

