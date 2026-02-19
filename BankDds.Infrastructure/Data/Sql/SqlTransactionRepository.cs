using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<SqlTransactionRepository> _logger;

    public SqlTransactionRepository(
        IConnectionStringProvider connectionStringProvider,
        IUserSession userSession,
        IAccountRepository accountRepository,
        ILogger<SqlTransactionRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
        _accountRepository = accountRepository;
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
    /// Transfers money between two accounts, routing on branch topology (GAP-02 fix).
    /// <list type="bullet">
    ///   <item><b>Same-branch</b> (sourceAccount.MACN == destAccount.MACN): single
    ///       ADO.NET connection + <see cref="SqlTransaction"/> on that server — no MSDTC needed.</item>
    ///   <item><b>Cross-branch</b> (different MACN): delegates to
    ///       <c>SP_CrossBranchTransfer</c> on the source-branch server, which uses a Linked Server
    ///       reference and <c>BEGIN DISTRIBUTED TRANSACTION</c> (MSDTC) to debit source,
    ///       credit destination, and record <c>GD_CHUYENTIEN</c> atomically.</item>
    /// </list>
    /// In both cases all business validation is performed before touching the database.
    /// </summary>
    public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
    {        sotkFrom = sotkFrom.Trim(); sotkTo = sotkTo.Trim(); manv = manv.Trim();
        _logger.LogInformation("Transfer: From={SotkFrom} To={SotkTo} Amount={Amount} MANV={MANV}",
                               sotkFrom, sotkTo, amount, manv);
        // ── Pre-validation (no DB round-trip) ────────────────────────────────
        if (amount <= 0)
            throw new InvalidOperationException("Transfer amount must be greater than 0.");

        if (sotkFrom == sotkTo)
            throw new InvalidOperationException("Cannot transfer to the same account.");

        // ── Pre-validate accounts (outside transaction — for rich error messages) ──
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

        // ── Route on branch topology ──────────────────────────────────────────
        // Branch codes come from the pre-validated account objects, not from _userSession,
        // so the correct physical server is always targeted regardless of session context.
        string sourceBranch = sourceAccount.MACN;
        string destBranch   = destAccount.MACN;

        return string.Equals(sourceBranch, destBranch, StringComparison.OrdinalIgnoreCase)
            ? await ExecuteSameBranchTransferAsync(sotkFrom, sotkTo, amount, manv, sourceBranch)
            : await ExecuteCrossBranchTransferAsync(sotkFrom, sotkTo, amount, manv, sourceBranch, destBranch);
    }

    /// <summary>
    /// Same-branch transfer: single ADO.NET connection and transaction on the branch server.
    /// All three SPs (debit, credit, GD_CHUYENTIEN INSERT) run atomically under one
    /// <see cref="SqlTransaction"/>. No MSDTC required.
    /// </summary>
    private async Task<bool> ExecuteSameBranchTransferAsync(
        string sotkFrom, string sotkTo, decimal amount, string manv, string branch)
    {
        _logger.LogInformation("Same-branch transfer: branch={Branch} From={SotkFrom} To={SotkTo}",
                               branch, sotkFrom, sotkTo);
        SqlConnection?  connection    = null;
        SqlTransaction? dbTransaction = null;

        try
        {
            connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branch));
            await connection.OpenAsync();

            dbTransaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // Step 1: Deduct from source account.
            // SP enforces a locked balance check — 0 rows means concurrent modification.
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

            // Step 3: Record GD_CHUYENTIEN row (committed atomically with both balance updates).
            using (var cmd = new SqlCommand("SP_CreateTransferTransaction", connection, dbTransaction)
                   { CommandType = CommandType.StoredProcedure })
            {
                cmd.Parameters.AddWithValue("@SOTK_FROM", sotkFrom);
                cmd.Parameters.AddWithValue("@SOTK_TO",   sotkTo);
                cmd.Parameters.AddWithValue("@Amount",    amount);
                cmd.Parameters.AddWithValue("@MANV",      manv);
                await cmd.ExecuteScalarAsync(); // returns MAGD; ignored here
            }

            await dbTransaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            if (dbTransaction != null)
            {
                try { await dbTransaction.RollbackAsync(); }
                catch { /* ignore secondary rollback failures */ }
            }
            if (ex is InvalidOperationException) throw;
            throw new InvalidOperationException($"Transfer failed: {ex.Message}", ex);
        }
        finally
        {
            dbTransaction?.Dispose();
            connection?.Dispose();
        }
    }

    /// <summary>
    /// Cross-branch transfer: calls <c>SP_CrossBranchTransfer</c> on the source-branch server.
    /// <para>
    /// That SP uses a SQL Server Linked Server reference to reach the destination server and wraps
    /// the debit, credit, and GD_CHUYENTIEN INSERT in <c>BEGIN DISTRIBUTED TRANSACTION</c>
    /// (requires MSDTC to be enabled on both servers).
    /// </para>
    /// <para>
    /// GD_CHUYENTIEN is recorded on the <paramref name="sourceBranch"/> server by the SP.
    /// </para>
    /// <para>
    /// If the SP or Linked Server is not configured, a clear setup-guidance error is thrown
    /// rather than silently failing with a generic SQL error.
    /// </para>
    /// </summary>
    private async Task<bool> ExecuteCrossBranchTransferAsync(
        string sotkFrom, string sotkTo, decimal amount, string manv,
        string sourceBranch, string destBranch)
    {
        _logger.LogInformation(
            "Cross-branch transfer: {SourceBranch}→{DestBranch} From={SotkFrom} To={SotkTo}",
            sourceBranch, destBranch, sotkFrom, sotkTo);
        try
        {
            using var connection = new SqlConnection(
                _connectionStringProvider.GetConnectionStringForBranch(sourceBranch));
            await connection.OpenAsync();

            using var cmd = new SqlCommand("SP_CrossBranchTransfer", connection)
            {
                CommandType    = CommandType.StoredProcedure,
                // Distributed transactions via MSDTC can take longer than local ones
                CommandTimeout = 60
            };
            cmd.Parameters.AddWithValue("@SOTK_CHUYEN", sotkFrom);
            cmd.Parameters.AddWithValue("@SOTK_NHAN",   sotkTo);
            cmd.Parameters.AddWithValue("@SOTIEN",      amount);
            cmd.Parameters.AddWithValue("@MANV",        manv);
            cmd.Parameters.AddWithValue("@DEST_BRANCH", destBranch);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }
        catch (SqlException ex) when (ex.Number == 2812)
        {
            // 2812: stored procedure not found
            _logger.LogError(ex, "Cross-branch SP missing: {SourceBranch}→{DestBranch}", sourceBranch, destBranch);
            throw new InvalidOperationException(
                $"MSDTC/Linked Server not configured: SP_CrossBranchTransfer not found on '{sourceBranch}'. " +
                "Please follow sql/01-schema.sql §C and docs/audit/99-final-db-readiness.md to create the SP and configure Linked Servers.",
                ex);
        }
        catch (SqlException ex) when (ex.Number is 8501 or 8517 or 7391)
        {
            // 8501/8517: MSDTC unavailable; 7391: distributed tx not supported
            _logger.LogError(ex, "MSDTC unavailable for cross-branch transfer: {SourceBranch}→{DestBranch}", sourceBranch, destBranch);
            throw new InvalidOperationException(
                $"MSDTC/Linked Server not configured. Cross-branch transfer from '{sourceBranch}' to '{destBranch}' " +
                "requires MSDTC to be running on both servers. " +
                "Please follow sql/01-schema.sql §C and docs/audit/99-final-db-readiness.md.",
                ex);
        }
        catch (SqlException ex) when (ex.Number is 7202 or 7399 or 7312)
        {
            // 7202: Linked Server not found; 7399: provider error; 7312: access denied on Linked Server
            _logger.LogError(ex, "Linked Server error for cross-branch transfer: {SourceBranch}→{DestBranch}", sourceBranch, destBranch);
            throw new InvalidOperationException(
                $"MSDTC/Linked Server not configured. Linked Server from '{sourceBranch}' to '{destBranch}' is not set up. " +
                "Please follow sql/01-schema.sql §C and docs/audit/99-final-db-readiness.md.",
                ex);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "Cross-branch transfer SQL error {Number}: {SourceBranch}→{DestBranch}", ex.Number, sourceBranch, destBranch);
            throw new InvalidOperationException(
                $"Cross-branch transfer failed (SQL error {ex.Number}): {ex.Message}", ex);
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
