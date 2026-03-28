using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Handles customer profile persistence and branch-scoped customer queries.
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly IUserSession _userSession;
        private readonly ILogger<CustomerRepository> _logger;

        /// <summary>
        /// Initializes CustomerRepository with required infrastructure dependencies.
        /// </summary>
        /// <param name="connectionStringProvider">Connection provider for resolving target SQL instances.</param>
        /// <param name="userSession">Current user session for branch-scoped connections.</param>
        /// <param name="logger">Logger instance for repository diagnostics.</param>
        public CustomerRepository(
            IConnectionStringProvider connectionStringProvider,
            IUserSession userSession,
            ILogger<CustomerRepository> logger)
        {
            _connectionStringProvider = connectionStringProvider;
            _userSession = userSession;
            _logger = logger;
        }

        private string GetConnectionString() =>
            // Logic: customer write operations run on the currently selected branch shard.
            _connectionStringProvider.GetConnectionStringForBranch(_userSession.SelectedBranch);

        public async Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
        {
            branchCode = branchCode.Trim();
            _logger.LogInformation("GetCustomersByBranch: branch={Branch}", branchCode);
            var customers = new List<Customer>();
            try
            {
                // Logic: per-branch customer list must read from target branch shard.
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
            _logger.LogInformation("GetAllCustomers: cross-branch via Publisher");
            var customers = new List<Customer>();
            try
            {
                // Logic: all-customer list is served by publisher aggregate database.
                using var connection = new SqlConnection(_connectionStringProvider.GetPublisherConnection());
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
                using var connection = new SqlConnection(_connectionStringProvider.GetPublisherConnection());
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetCustomerByCMND", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CMND", cmnd);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync()) return MapFromReader(reader);
            }
            catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving customer '{cmnd}': {ex.Message}", ex); }
            return null;
        }

        public async Task<Customer?> GetCustomerByCMNDFromBranchAsync(string cmnd, string branchCode)
        {
            try
            {
                using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(branchCode));
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_GetCustomerByCMND", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CMND", cmnd);
                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync()) return MapFromReader(reader);
            }
            catch (SqlException ex) { throw new InvalidOperationException($"Database error retrieving branch customer '{cmnd}' from '{branchCode}': {ex.Message}", ex); }
            return null;
        }

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            try
            {
                using var connection = new SqlConnection(_connectionStringProvider.GetConnectionStringForBranch(customer.MaCN));
                await connection.OpenAsync();
                using var command = new SqlCommand("SP_AddCustomer", connection) { CommandType = CommandType.StoredProcedure };
                command.Parameters.AddWithValue("@CMND", customer.CMND);
                command.Parameters.AddWithValue("@HO", customer.Ho);
                command.Parameters.AddWithValue("@TEN", customer.Ten);
                command.Parameters.AddWithValue("@NGAYSINH", (object?)customer.NgaySinh ?? DBNull.Value);
                command.Parameters.AddWithValue("@DIACHI", (object?)customer.DiaChi ?? DBNull.Value);
                command.Parameters.AddWithValue("@NGAYCAP", (object?)customer.NgayCap ?? DBNull.Value);
                command.Parameters.AddWithValue("@SODT", (object?)customer.SODT ?? DBNull.Value);
                command.Parameters.AddWithValue("@PHAI", customer.Phai);
                command.Parameters.AddWithValue("@MACN", customer.MaCN);
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
                command.Parameters.AddWithValue("@CMND", customer.CMND);
                command.Parameters.AddWithValue("@HO", customer.Ho);
                command.Parameters.AddWithValue("@TEN", customer.Ten);
                command.Parameters.AddWithValue("@NGAYSINH", (object?)customer.NgaySinh ?? DBNull.Value);
                command.Parameters.AddWithValue("@DIACHI", (object?)customer.DiaChi ?? DBNull.Value);
                command.Parameters.AddWithValue("@NGAYCAP", (object?)customer.NgayCap ?? DBNull.Value);
                command.Parameters.AddWithValue("@SODT", (object?)customer.SODT ?? DBNull.Value);
                command.Parameters.AddWithValue("@PHAI", customer.Phai);
                command.Parameters.AddWithValue("@MACN", customer.MaCN);
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
            CMND = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
            Ho = reader.GetString(reader.GetOrdinal("HO")),
            Ten = reader.GetString(reader.GetOrdinal("TEN")),
            NgaySinh = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
            DiaChi = reader.IsDBNull(reader.GetOrdinal("DIACHI")) ? "" : reader.GetString(reader.GetOrdinal("DIACHI")),
            NgayCap = reader.IsDBNull(reader.GetOrdinal("NGAYCAP")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
            SODT = reader.IsDBNull(reader.GetOrdinal("SODT")) ? "" : reader.GetString(reader.GetOrdinal("SODT")),
            Phai = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
            MaCN = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
            TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("TrangThaiXoa")))
        };
    }
}
