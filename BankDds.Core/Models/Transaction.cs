namespace BankDds.Core.Models;

public class Transaction
{
    public string MAGD { get; set; } = string.Empty;
    public string SOTK { get; set; } = string.Empty;
    public string LOAIGD { get; set; } = string.Empty; // GT (deposit) or RT (withdraw)
    public DateTime NGAYGD { get; set; }
    public decimal SOTIEN { get; set; }
    public int MANV { get; set; }
    public string? SOTK_NHAN { get; set; } // For transfers
}
