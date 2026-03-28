using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Handles transaction persistence for deposit, withdrawal, transfer, and daily totals.
    /// </summary>
    public class TransactionRepository : ITransactionRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IUserSession _userSession;
        private readonly ILogger<TransactionRepository> _logger;
        private static readonly TimeSpan SqlDateTimeEndOfDayOffset = TimeSpan.FromMilliseconds(997);

        /// <summary>
        /// Initializes TransactionRepository with required infrastructure dependencies.
        /// </summary>
        /// <param name="connectionStringProvider">Connection provider for resolving target SQL instances.</param>
        /// <param name="userSession">Current user session for branch-scoped connections.</param>
        /// <param name="logger">Logger instance for repository diagnostics.</param>
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
            // Logic: transaction write operations must target the session-selected branch shard.
            return _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);
        }

        private static DateTime? NormalizeStartDate(DateTime? value) => value?.Date;

        private static DateTime? NormalizeInclusiveEndDate(DateTime? value) =>
            value?.Date.AddDays(1).Subtract(SqlDateTimeEndOfDayOffset);

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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy giao dịch: {ex.Message}", ex);
            }
            return transactions;
        }

        public async Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null)
        {
            var transactions = new List<Transaction>();
            var normalizedFromDate = NormalizeStartDate(fromDate);
            var normalizedToDate = NormalizeInclusiveEndDate(toDate);
            try
            {
                // Logic: branch transaction history reads from requested branch shard only.
                using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetTransactionsByBranch", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@MACN", branchCode);
                command.Parameters.AddWithValue("@FromDate", normalizedFromDate ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ToDate", normalizedToDate ?? (object)DBNull.Value);
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add(MapFromReader(reader));
                }
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy giao dịch: {ex.Message}", ex);
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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy tổng rút tiền trong ngày: {ex.Message}", ex);
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
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi lấy tổng chuyển tiền trong ngày: {ex.Message}", ex);
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
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi gửi tiền: {ex.Message}", ex);
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
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Lỗi cơ sở dữ liệu khi rút tiền: {ex.Message}", ex);
            }
        }

        public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
        {
            sotkFrom = sotkFrom.Trim(); sotkTo = sotkTo.Trim(); manv = manv.Trim();
            _logger.LogInformation("Transfer: From={SotkFrom} To={SotkTo} Amount={Amount} MANV={MANV}",
                                   sotkFrom, sotkTo, amount, manv);
            if (amount <= 0)
                throw new InvalidOperationException("Số tiền chuyển phải lớn hơn 0.");
            if (sotkFrom == sotkTo)
                throw new InvalidOperationException("Không thể chuyển đến cùng một tài khoản.");
            try
            {
                using var connection = new SqlConnection(GetConnectionString());
                await connection.OpenAsync();
                using var cmd = new SqlCommand("SP_CrossBranchTransfer", connection)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 60
                };
                cmd.Parameters.AddWithValue("@SOTK_CHUYEN", sotkFrom);
                cmd.Parameters.AddWithValue("@SOTK_NHAN", sotkTo);
                cmd.Parameters.AddWithValue("@SOTIEN", amount);
                cmd.Parameters.AddWithValue("@MANV", manv);
                await cmd.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 2812)
            {
                _logger.LogError(ex, "SP_CrossBranchTransfer not found on branch {Branch}",
                                 _userSession.SelectedBranch);
                throw new InvalidOperationException(
                     $"Không tìm thấy SP_CrossBranchTransfer tại chi nhánh '{_userSession.SelectedBranch}'. " +
                    "Hãy đảm bảo SP đã được replicate từ Publisher.",
                    ex);
            }
            catch (SqlException ex) when (ex.Number is 8501 or 8517 or 7391)
            {
                _logger.LogError(ex, "MSDTC error during cross-branch transfer");
                throw new InvalidOperationException(
                    "MSDTC chưa sẵn sàng cho chuyển khoản liên chi nhánh. " +
                    "Hãy đảm bảo dịch vụ Distributed Transaction Coordinator đang chạy trên cả hai server.",
                    ex);
            }
            catch (SqlException ex) when (ex.Number is 7202 or 7399 or 7312)
            {
                _logger.LogError(ex, "Linked Server (LINK1) error during cross-branch transfer");
                throw new InvalidOperationException(
                    "Linked Server LINK1 chưa được cấu hình. " +
                    "Hãy cấu hình Linked Server LINK1 theo hướng dẫn SSMS UI trong tài liệu dự án.",
                    ex);
            }
            catch (SqlException ex) when (ex.Number >= 50000)
            {
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
