namespace BankDds.Core.Models;

/// <summary>
/// Account statement report model matching DE3 requirements
/// </summary>
public class AccountStatement
{
    public string SOTK { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }  // Balance at FromDate - 1
    public List<StatementLine> Lines { get; set; } = new();
    public decimal ClosingBalance { get; set; }  // Balance at ToDate end-of-day
}

/// <summary>
/// One row in the DE3 account statement table.
/// Columns: Số dư đầu | Ngày | Loại GD | Số tiền | Số dư sau
/// </summary>
public class StatementLine
{
    /// <summary>Balance before this transaction ("Số dư đầu").</summary>
    public decimal OpeningBalance { get; set; }
    public DateTime Date { get; set; }
    /// <summary>Transaction type normalised to GT / RT / CT (never CK).</summary>
    public string TransactionType { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    /// <summary>Balance after this transaction ("Số dư sau").</summary>
    public decimal RunningBalance { get; set; }
    public string Description { get; set; } = string.Empty;
    /// <summary>True for RT and outgoing CT (sender side).</summary>
    public bool IsDebit { get; set; }

    // Display helpers
    public string TypeDisplay => TransactionType switch
    {
        "GT" => "GT (Gửi tiền)",
        "RT" => "RT (Rút tiền)",
        "CT" => "CT (Chuyển tiền)",
        _    => TransactionType
    };

    public string AmountDisplay => IsDebit ? $"-{Amount:N0}" : $"+{Amount:N0}";
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
