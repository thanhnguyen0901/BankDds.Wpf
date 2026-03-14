namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a posted deposit, withdrawal, or transfer transaction entry.
    /// </summary>
    public class Transaction
    {
        public int MAGD { get; set; }
        public string SOTK { get; set; } = string.Empty;
        public string LOAIGD { get; set; } = string.Empty;
        public DateTime NGAYGD { get; set; }
        public decimal SOTIEN { get; set; }
        public string MANV { get; set; } = string.Empty;
        public string? SOTK_NHAN { get; set; }
        public string Status { get; set; } = "Completed";
        public string? ErrorMessage { get; set; }
        public string StatusDisplay => Status switch
        {
            "Completed" => "Completed",
            "Failed" => "Failed",
            "Pending" => "Pending",
            _ => Status
        };
    }
}
