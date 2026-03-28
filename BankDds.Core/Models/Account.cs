using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a customer bank account used for balance and transaction operations.
    /// </summary>
    public class Account : ObservableModel
    {
        private string _sotk = string.Empty;
        private string _cmnd = string.Empty;
        private decimal _sodu;
        private string _macn = string.Empty;
        private DateTime _ngayMoTk;
        private string _status = "Active";

        public string SOTK
        {
            get => _sotk;
            set => SetProperty(ref _sotk, value, nameof(StatementDisplayText));
        }

        public string CMND
        {
            get => _cmnd;
            set => SetProperty(ref _cmnd, value);
        }

        public decimal SODU
        {
            get => _sodu;
            set => SetProperty(ref _sodu, value);
        }

        public string MACN
        {
            get => _macn;
            set => SetProperty(ref _macn, value, nameof(BranchDisplayName), nameof(StatementDisplayText));
        }

        public DateTime NGAYMOTK
        {
            get => _ngayMoTk;
            set => SetProperty(ref _ngayMoTk, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value, nameof(StatusDisplay));
        }

        public string BranchDisplayName => DisplayText.Branch(MACN);
        public string StatementDisplayText => $"{SOTK} - {BranchDisplayName}";
        public string StatusDisplay => DisplayText.AccountStatus(Status);
    }
}
