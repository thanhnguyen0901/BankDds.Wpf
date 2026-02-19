namespace BankDds.Core.Models;

/// <summary>
/// CHINHANH — bank branch reference entity.
/// Stored in Bank_Main (SERVER3); branch codes are the topology keys used throughout the system.
/// </summary>
public class Branch
{
    /// <summary>nChar(10) — branch code, e.g. "BENTHANH"</summary>
    public string MACN { get; set; } = string.Empty;

    /// <summary>nvarchar(100) — full branch name, e.g. "Bến Thành" (max 100 chars, UNIQUE)</summary>
    public string TENCN { get; set; } = string.Empty;

    /// <summary>nvarchar(100) — branch address</summary>
    public string DiaChi { get; set; } = string.Empty;

    /// <summary>nvarchar(15) — branch phone number</summary>
    public string SODT { get; set; } = string.Empty;
}
