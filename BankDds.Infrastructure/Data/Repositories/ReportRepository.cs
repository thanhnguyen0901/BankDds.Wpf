using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Handles report data queries for statements, account listings, and transaction summaries.
    /// </summary>
    public class ReportRepository : IReportRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly ILogger<ReportRepository> _logger;

        /// <summary>
        /// Initializes ReportRepository with required infrastructure dependencies.
        /// </summary>
        /// <param name="connectionStringProvider">Connection provider for resolving target SQL instances.</param>
        /// <param name="logger">Logger instance for repository diagnostics.</param>
        public ReportRepository(
            IConnectionStringProvider connectionStringProvider,
            ILogger<ReportRepository> logger)
        {
            _connectionStringProvider = connectionStringProvider;
            _logger = logger;
        }

        private string GetConnectionString()
        {
            return _connectionStringProvider.GetPublisherConnection();
        }

        public async Task<AccountStatement?> GetAccountStatementAsync(string accountNumber, DateTime fromDate, DateTime toDate)
        {
            accountNumber = accountNumber.Trim();
            _logger.LogInformation("Report: AccountStatement SOTK={SOTK} period={From:yyyy-MM-dd}->{To:yyyy-MM-dd}",
                                   accountNumber, fromDate, toDate);
            if (fromDate.Date > toDate.Date)
                throw new ArgumentException("Từ ngày phải nhỏ hơn hoặc bằng đến ngày.");
            var endOfDay = toDate.Date.AddDays(1).AddMilliseconds(-3);
            try
            {
                using var connection = new SqlConnection(GetConnectionString());
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetAccountStatement", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@SOTK", accountNumber);
                command.Parameters.AddWithValue("@TuNgay", fromDate.Date);
                command.Parameters.AddWithValue("@DenNgay", endOfDay);
                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return null;
                var statement = new AccountStatement
                {
                    SOTK = reader.GetString(reader.GetOrdinal("SOTK")),
                    FromDate = fromDate.Date,
                    ToDate = toDate.Date,
                    OpeningBalance = reader.GetDecimal(reader.GetOrdinal("OpeningBalance"))
                };
                if (await reader.NextResultAsync())
                {
                    var lines = new List<StatementLine>();
                    while (await reader.ReadAsync())
                    {
                        lines.Add(new StatementLine
                        {
                            OpeningBalance = reader.GetDecimal(reader.GetOrdinal("OpeningBal")),
                            Date = reader.GetDateTime(reader.GetOrdinal("NGAYGD")),
                            TransactionType = reader.GetString(reader.GetOrdinal("LOAIGD")),
                            TransactionId = reader.GetInt32(reader.GetOrdinal("MAGD")).ToString(),
                            Amount = reader.GetDecimal(reader.GetOrdinal("SOTIEN")),
                            RunningBalance = reader.GetDecimal(reader.GetOrdinal("RunningBalance")),
                            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                            IsDebit = reader.GetBoolean(reader.GetOrdinal("IsDebit"))
                        });
                    }
                    statement.Lines = lines;
                    statement.ClosingBalance = lines.Count > 0 ? lines[^1].RunningBalance : statement.OpeningBalance;
                }
                return statement;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi tạo sao kê tài khoản: {ex.Message}", ex);
            }
        }

        public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
        {
            _logger.LogInformation("Report: AccountsOpenedInPeriod branch={Branch} period={From:yyyy-MM-dd}->{To:yyyy-MM-dd}",
                                   branchCode ?? "ALL", fromDate, toDate);
            var accounts = new List<Account>();
            try
            {
                var connStr = (string.IsNullOrEmpty(branchCode) || branchCode == "ALL")
                    ? _connectionStringProvider.GetPublisherConnection()
                    : _connectionStringProvider.GetConnectionStringForBranch(branchCode);
                using var connection = new SqlConnection(connStr);
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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy danh sách tài khoản: {ex.Message}", ex);
            }
            return accounts;
        }

        public async Task<List<Customer>> GetCustomersByBranchAsync(string? branchCode = null)
        {
            _logger.LogInformation("Report: CustomersByBranch branch={Branch}", branchCode ?? "ALL");
            var customers = new List<Customer>();
            try
            {
                var connStr = (string.IsNullOrEmpty(branchCode) || branchCode == "ALL")
                    ? _connectionStringProvider.GetPublisherConnection()
                    : _connectionStringProvider.GetConnectionStringForBranch(branchCode);
                using var connection = new SqlConnection(connStr);
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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy danh sách khách hàng: {ex.Message}", ex);
            }
            return customers;
        }

        public async Task<TransactionSummary?> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
        {
            _logger.LogInformation("Report: TransactionSummary branch={Branch} period={From:yyyy-MM-dd}->{To:yyyy-MM-dd}",
                                   branchCode ?? "ALL", fromDate, toDate);
            try
            {
                var connStr = (string.IsNullOrEmpty(branchCode) || branchCode == "ALL")
                    ? _connectionStringProvider.GetPublisherConnection()
                    : _connectionStringProvider.GetConnectionStringForBranch(branchCode);
                using var connection = new SqlConnection(connStr);
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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi tạo tổng hợp giao dịch: {ex.Message}", ex);
            }
        }

        private Account MapAccountFromReader(SqlDataReader reader)
        {
            return new Account
            {
                SOTK = reader.GetString(reader.GetOrdinal("SOTK")).Trim(),
                CMND = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
                SODU = reader.GetDecimal(reader.GetOrdinal("SODU")),
                MACN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
                NGAYMOTK = reader.GetDateTime(reader.GetOrdinal("NGAYMOTK")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Active" : reader.GetString(reader.GetOrdinal("Status")).Trim()
            };
        }

        private Customer MapCustomerFromReader(SqlDataReader reader)
        {
            return new Customer
            {
                CMND = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
                Ho = reader.GetString(reader.GetOrdinal("HO")),
                Ten = reader.GetString(reader.GetOrdinal("TEN")),
                NgaySinh = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
                DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
                NgayCap = reader.IsDBNull(reader.GetOrdinal("NGAYCAP")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
                SODT = reader.IsDBNull(reader.GetOrdinal("SODT")) ? "" : reader.GetString(reader.GetOrdinal("SODT")),
                Phai = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
                MaCN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
                TrangThaiXoa = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("TrangThaiXoa")))
            };
        }

        private Transaction MapTransactionFromReader(SqlDataReader reader)
        {
            return new Transaction
            {
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
}
