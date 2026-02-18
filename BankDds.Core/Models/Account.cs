namespace BankDds.Core.Models;

public class Account
{
    public string SOTK { get; set; } = string.Empty;
    public string CMND { get; set; } = string.Empty;
    public decimal SODU { get; set; }
    public string MACN { get; set; } = string.Empty;
    public DateTime NGAYMOTK { get; set; }
    public string Status { get; set; } = "Active"; // Active or Closed
    
    public string StatusDisplay => Status == "Active" ? "Active" : "Closed";
}
