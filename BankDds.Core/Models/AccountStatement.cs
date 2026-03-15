using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents statement data of a single account for a selected time range.
    /// </summary>
    public class AccountStatement
    {
        public string SOTK { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal OpeningBalance { get; set; }
        public List<StatementLine> Lines { get; set; } = new();
        public decimal ClosingBalance { get; set; }
    }

    /// <summary>
    /// Represents one statement row of a transaction in chronological order.
    /// </summary>
    public class StatementLine
    {
        public decimal OpeningBalance { get; set; }
        public DateTime Date { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsDebit { get; set; }

        public string TypeDisplay => TransactionType switch
        {
            "GT" => "GT (Gửi tiền)",
            "RT" => "RT (Rút tiền)",
            "CT" => "CT (Chuyển tiền)",
            _ => TransactionType
        };

        public string AmountDisplay => IsDebit ? $"-{Amount:N0}" : $"+{Amount:N0}";
    }

    /// <summary>
    /// Represents aggregated transaction statistics and detail list for reporting.
    /// </summary>
    public class TransactionSummary
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string? BranchCode { get; set; }
        public string BranchDisplay => DisplayText.Branch(BranchCode);
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
}
