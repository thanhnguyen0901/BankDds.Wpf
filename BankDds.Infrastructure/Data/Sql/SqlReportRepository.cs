using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// SQL Server implementation of IReportRepository
/// </summary>
public class SqlReportRepository : IReportRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly IUserSession _userSession;

    public SqlReportRepository(IConnectionStringProvider connectionStringProvider, IUserSession userSession)
    {
        _connectionStringProvider = connectionStringProvider;
        _userSession = userSession;
    }

    private string GetConnectionString()
    {
        return _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);
    }

    /// <summary>
    /// Gets an account statement matching the DE3 requirement (SP_GetAccountStatement).
    ///
    /// SP contract — the stored procedure must accept:
    ///   @SOTK    nChar(9)
    ///   @TuNgay  DateTime   — start of period (calendar day, time stripped by caller)
    ///   @DenNgay DateTime   — end of period (end-of-day: date + 23:59:59.997, set by caller)
    ///
    /// SP must return TWO result sets:
    ///
    ///   Result set 1 (single row):
    ///     SOTK          nChar(9)
    ///     OpeningBalance money       — balance at end of (@TuNgay − 1 day), i.e. just before period starts
    ///
    ///   Result set 2 (one row per transaction, sorted NGAYGD ASC):
    ///     MAGD          nvarchar     — transaction ID
    ///     NGAYGD        DateTime
    ///     LOAIGD        nChar(2)     — GT / RT / CT  (SP must normalise CK → CT)
    ///     SOTIEN        money        — absolute transaction amount (always positive)
    ///     OpeningBal    money        — balance BEFORE this transaction ("Số dư đầu")
    ///     RunningBalance money       — balance AFTER  this transaction ("Số dư sau")
    ///     Description   nvarchar     — human-readable description
    ///     IsDebit       bit          — 1 for RT and outgoing CT
    ///
    /// Reference logic for SP_GetAccountStatement (T-SQL outline):
    ///
    ///   DECLARE @OpeningBal money =
    ///     (SELECT SODU FROM TAIKHOAN WHERE SOTK = @SOTK)
    ///     -- subtract every completed transaction >= @TuNgay that affected this account:
    ///     + ISNULL((SELECT SUM(SOTIEN) FROM GD_GOIRUT  WHERE SOTK=@SOTK AND LOAIGD='RT' AND NGAYGD>=@TuNgay AND Status='Completed'),0)
    ///     + ISNULL((SELECT SUM(SOTIEN) FROM GD_CHUYENTIEN WHERE SOTK_CHUYEN=@SOTK AND NGAYGD>=@TuNgay AND Status='Completed'),0)
    ///     - ISNULL((SELECT SUM(SOTIEN) FROM GD_GOIRUT  WHERE SOTK=@SOTK AND LOAIGD='GT' AND NGAYGD>=@TuNgay AND Status='Completed'),0)
    ///     - ISNULL((SELECT SUM(SOTIEN) FROM GD_CHUYENTIEN WHERE SOTK_NHAN=@SOTK AND NGAYGD>=@TuNgay AND Status='Completed'),0);
    ///
    ///   SELECT @SOTK AS SOTK, @OpeningBal AS OpeningBalance;
    ///
    ///   -- Then build the per-row running balance using a window function or cursor.
    /// </summary>
    public async Task<AccountStatement?> GetAccountStatementAsync(string accountNumber, DateTime fromDate, DateTime toDate)
    {
        if (fromDate.Date > toDate.Date)
            throw new ArgumentException("FromDate must be less than or equal to ToDate.");

        // End-of-day: include every transaction with time up to 23:59:59.997 on toDate.
        var endOfDay = toDate.Date.AddDays(1).AddMilliseconds(-3);

        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAccountStatement", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@SOTK",    accountNumber);
            command.Parameters.AddWithValue("@TuNgay",  fromDate.Date);
            command.Parameters.AddWithValue("@DenNgay", endOfDay);

            using var reader = await command.ExecuteReaderAsync();

            // ── Result set 1: account info + opening balance ───────────────────
            if (!await reader.ReadAsync())
                return null;

            var statement = new AccountStatement
            {
                SOTK           = reader.GetString(reader.GetOrdinal("SOTK")),
                FromDate       = fromDate.Date,
                ToDate         = toDate.Date,
                OpeningBalance = reader.GetDecimal(reader.GetOrdinal("OpeningBalance"))
            };

            // ── Result set 2: per-transaction lines (sorted ASC by SP) ─────────
            if (await reader.NextResultAsync())
            {
                var lines = new List<StatementLine>();

                while (await reader.ReadAsync())
                {
                    lines.Add(new StatementLine
                    {
                        OpeningBalance  = reader.GetDecimal(reader.GetOrdinal("OpeningBal")),
                        Date            = reader.GetDateTime(reader.GetOrdinal("NGAYGD")),
                        TransactionType = reader.GetString(reader.GetOrdinal("LOAIGD")),
                        TransactionId   = reader.GetString(reader.GetOrdinal("MAGD")),
                        Amount          = reader.GetDecimal(reader.GetOrdinal("SOTIEN")),
                        RunningBalance  = reader.GetDecimal(reader.GetOrdinal("RunningBalance")),
                        Description     = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                        IsDebit         = reader.GetBoolean(reader.GetOrdinal("IsDebit"))
                    });
                }

                statement.Lines          = lines;
                statement.ClosingBalance = lines.Count > 0 ? lines[^1].RunningBalance : statement.OpeningBalance;
            }

            return statement;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error generating account statement: {ex.Message}", ex);
        }
    }

    public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        var accounts = new List<Account>();

        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetAccountsOpenedInPeriod", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@FromDate", fromDate);
            command.Parameters.AddWithValue("@ToDate", toDate);
            command.Parameters.AddWithValue("@BranchCode", branchCode ?? (object)DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                accounts.Add(MapAccountFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving accounts: {ex.Message}", ex);
        }

        return accounts;
    }

    public async Task<List<Customer>> GetCustomersByBranchAsync(string? branchCode = null)
    {
        var customers = new List<Customer>();

        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetCustomersByBranch", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@BranchCode", branchCode ?? (object)DBNull.Value);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                customers.Add(MapCustomerFromReader(reader));
            }
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error retrieving customers: {ex.Message}", ex);
        }

        return customers;
    }

    public async Task<TransactionSummary?> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        try
        {
            using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = new SqlCommand("SP_GetTransactionSummary", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@FromDate", fromDate);
            command.Parameters.AddWithValue("@ToDate", toDate);
            command.Parameters.AddWithValue("@BranchCode", branchCode ?? (object)DBNull.Value);

            var summary = new TransactionSummary
            {
                FromDate = fromDate,
                ToDate = toDate,
                BranchCode = branchCode
            };

            using var reader = await command.ExecuteReaderAsync();

            // First result set: Summary counts and totals
            if (await reader.ReadAsync())
            {
                summary.TotalTransactionCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
                summary.DepositCount = reader.GetInt32(reader.GetOrdinal("DepositCount"));
                summary.WithdrawalCount = reader.GetInt32(reader.GetOrdinal("WithdrawalCount"));
                summary.TransferCount = reader.GetInt32(reader.GetOrdinal("TransferCount"));
                summary.TotalDepositAmount = reader.GetDecimal(reader.GetOrdinal("TotalDepositAmount"));
                summary.TotalWithdrawalAmount = reader.GetDecimal(reader.GetOrdinal("TotalWithdrawalAmount"));
                summary.TotalTransferAmount = reader.GetDecimal(reader.GetOrdinal("TotalTransferAmount"));
            }

            // Second result set: Transaction details
            if (await reader.NextResultAsync())
            {
                var transactions = new List<Transaction>();
                while (await reader.ReadAsync())
                {
                    transactions.Add(MapTransactionFromReader(reader));
                }
                summary.Transactions = transactions;
            }

            return summary;
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException($"Database error generating transaction summary: {ex.Message}", ex);
        }
    }

    private Account MapAccountFromReader(SqlDataReader reader)
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

    private Customer MapCustomerFromReader(SqlDataReader reader)
    {
        return new Customer
        {
            CMND = reader.GetString(reader.GetOrdinal("CMND")),
            Ho = reader.GetString(reader.GetOrdinal("HO")),
            Ten = reader.GetString(reader.GetOrdinal("TEN")),
            NgaySinh = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
            DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
            NgayCap = reader.IsDBNull(reader.GetOrdinal("NGAYCAP")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
            SDT = reader.IsDBNull(reader.GetOrdinal("SDT")) ? "" : reader.GetString(reader.GetOrdinal("SDT")),
            Phai = reader.GetString(reader.GetOrdinal("PHAI")),
            MaCN = reader.GetString(reader.GetOrdinal("MACN")),
            TrangThaiXoa = reader.GetInt32(reader.GetOrdinal("TrangThaiXoa"))
        };
    }

    private Transaction MapTransactionFromReader(SqlDataReader reader)
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
