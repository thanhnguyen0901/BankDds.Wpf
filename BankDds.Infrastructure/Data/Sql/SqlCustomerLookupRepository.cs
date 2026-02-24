using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace BankDds.Infrastructure.Data.Sql;

/// <summary>
/// Read-only customer lookup against the lookup subscriber (NGANHANG_TRACUU).
/// Uses direct SELECT on the replicated KHACHHANG table — the subscriber's
/// security script (08_subscribers_post_replication_fixups.sql §7) grants
/// SELECT to all three roles on that database.
/// </summary>
public class SqlCustomerLookupRepository : ICustomerLookupRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;
    private readonly ILogger<SqlCustomerLookupRepository> _logger;

    public SqlCustomerLookupRepository(
        IConnectionStringProvider connectionStringProvider,
        ILogger<SqlCustomerLookupRepository> logger)
    {
        _connectionStringProvider = connectionStringProvider;
        _logger = logger;
    }

    /// <summary>
    /// Returns the lookup connection string.  Falls back to Publisher when
    /// the LookupDatabase key is not configured (so dev/InMemory scenarios still work).
    /// </summary>
    private string GetConnectionString()
    {
        return _connectionStringProvider.GetLookupConnection()
            ?? _connectionStringProvider.GetPublisherConnection();
    }

    /// <inheritdoc />
    public async Task<Customer?> GetCustomerByCmndAsync(string cmnd)
    {
        cmnd = cmnd.Trim();
        _logger.LogInformation("CustomerLookup: lookup CMND={Cmnd}", cmnd);

        try
        {
            await using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            const string sql = """
                SELECT TOP 1
                       CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
                FROM   dbo.KHACHHANG
                WHERE  CMND = @CMND
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@CMND", SqlDbType.NChar, 10).Value = cmnd;

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return MapFromReader(reader);
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"CustomerLookup: database error looking up CMND '{cmnd}': {ex.Message}", ex);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50)
    {
        keyword = keyword.Trim();
        _logger.LogInformation("CustomerLookup: search name keyword={Keyword}, max={Max}", keyword, maxResults);

        var results = new List<Customer>();

        try
        {
            await using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();

            // LIKE search on concatenated HO + ' ' + TEN.
            // The % wildcards are added server-side to prevent injection via the parameter.
            const string sql = """
                SELECT TOP (@MaxRows)
                       CMND, HO, TEN, NGAYSINH, DIACHI, NGAYCAP, SODT, PHAI, MACN, TrangThaiXoa
                FROM   dbo.KHACHHANG
                WHERE  (HO + N' ' + TEN) LIKE N'%' + @Keyword + N'%'
                ORDER  BY HO, TEN
                """;

            await using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxResults;
            cmd.Parameters.Add("@Keyword", SqlDbType.NVarChar, 100).Value = keyword;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(MapFromReader(reader));
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                $"CustomerLookup: database error searching name '{keyword}': {ex.Message}", ex);
        }

        return results;
    }

    // ────────────────── mapping ──────────────────

    private static Customer MapFromReader(SqlDataReader reader) => new()
    {
        CMND         = reader.GetString(reader.GetOrdinal("CMND")).Trim(),
        Ho           = reader.GetString(reader.GetOrdinal("HO")),
        Ten          = reader.GetString(reader.GetOrdinal("TEN")),
        NgaySinh     = reader.IsDBNull(reader.GetOrdinal("NGAYSINH")) ? null : reader.GetDateTime(reader.GetOrdinal("NGAYSINH")),
        DiaChi       = reader.IsDBNull(reader.GetOrdinal("DIACHI"))   ? ""   : reader.GetString(reader.GetOrdinal("DIACHI")),
        NgayCap      = reader.IsDBNull(reader.GetOrdinal("NGAYCAP"))  ? null : reader.GetDateTime(reader.GetOrdinal("NGAYCAP")),
        SODT         = reader.IsDBNull(reader.GetOrdinal("SODT"))     ? ""   : reader.GetString(reader.GetOrdinal("SODT")),
        Phai         = reader.GetString(reader.GetOrdinal("PHAI")).Trim(),
        MaCN         = reader.GetString(reader.GetOrdinal("MACN")).Trim(),
        TrangThaiXoa = reader.IsDBNull(reader.GetOrdinal("TrangThaiXoa")) ? 0 : reader.GetInt32(reader.GetOrdinal("TrangThaiXoa"))
    };
}
