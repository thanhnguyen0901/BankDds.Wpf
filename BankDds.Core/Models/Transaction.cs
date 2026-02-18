namespace BankDds.Core.Models;

public class Transaction
{
    public string MAGD { get; set; } = string.Empty;
    public string SOTK { get; set; } = string.Empty;
    public string LOAIGD { get; set; } = string.Empty; // GT (Gửi tiền), RT (Rút tiền), CT (Chuyển tiền). Legacy value "CK" is treated as CT throughout the application.
    public DateTime NGAYGD { get; set; }
    public decimal SOTIEN { get; set; }
    public string MANV { get; set; } = string.Empty;
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
