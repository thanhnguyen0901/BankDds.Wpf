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

public class TransactionSummary
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string? BranchCode { get; set; }
    public string BranchDisplay => BranchCode ?? "ALL";
    
    public int TotalTransactionCount { get; set; }
    public int DepositCount { get; set; }
    public int WithdrawalCount { get; set; }
    public int TransferCount { get; set; }
    
    public decimal TotalDepositAmount { get; set; }
    public decimal TotalWithdrawalAmount { get; set; }
    public decimal TotalTransferAmount { get; set; }
    public decimal TotalAmount => TotalDepositAmount + TotalWithdrawalAmount + TotalTransferAmount;
    
    public List<Transaction> Transactions { get; set; } = new();
}
