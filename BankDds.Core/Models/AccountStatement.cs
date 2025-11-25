namespace BankDds.Core.Models;

public class AccountStatement
{
    public string SOTK { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<Transaction> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
}
