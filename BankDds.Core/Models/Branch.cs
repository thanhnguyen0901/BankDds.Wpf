namespace BankDds.Core.Models;

/// <summary>
/// CHINHANH — bank branch reference entity.
/// Stored in Bank_Main (SERVER3); branch codes are the topology keys used throughout the system.
/// </summary>
public class Branch
{
    /// <summary>nChar(10) — branch code, e.g. "BENTHANH"</summary>
    public string MACN { get; set; } = string.Empty;

    /// <summary>nvarchar(50) — full branch name, e.g. "Bến Thành"</summary>
    public string TENCN { get; set; } = string.Empty;

    /// <summary>nvarchar(100) — branch address</summary>
    public string DiaChi { get; set; } = string.Empty;

    /// <summary>varchar(15) — branch phone number</summary>
    public string SoDT { get; set; } = string.Empty;
}
