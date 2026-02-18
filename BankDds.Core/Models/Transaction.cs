namespace BankDds.Core.Models;

public class Transaction
{
    public string MAGD { get; set; } = string.Empty;
    public string SOTK { get; set; } = string.Empty;
    public string LOAIGD { get; set; } = string.Empty; // GT (deposit) or RT (withdraw) or CK (transfer)
    public DateTime NGAYGD { get; set; }
    public decimal SOTIEN { get; set; }
    public int MANV { get; set; }
    public string? SOTK_NHAN { get; set; } // For transfers

    // Status tracking
    public string Status { get; set; } = "Completed"; // Pending, Completed, Failed
    public string? ErrorMessage { get; set; } // Capture error details for failed transactions

    // Display property for status with visual indicator
    public string StatusDisplay => Status switch
    {
        "Completed" => "Completed",
        "Failed" => "Failed",
        "Pending" => "Pending",
        _ => Status
    };
}
