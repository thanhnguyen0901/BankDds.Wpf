using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a branch employee who performs operations and transactions in the system.
    /// </summary>
    public class Employee
    {
        public string MANV { get; set; } = string.Empty;
        public string HO { get; set; } = string.Empty;
        public string TEN { get; set; } = string.Empty;
        public string DIACHI { get; set; } = string.Empty;
        public string CMND { get; set; } = string.Empty;
        public string PHAI { get; set; } = string.Empty;
        public string SODT { get; set; } = string.Empty;
        public string SDT
        {
            get => SODT;
            set => SODT = value;
        }
        public string MACN { get; set; } = string.Empty;
        public int TrangThaiXoa { get; set; } = 0;
        public string FullName => $"{HO} {TEN}";
        public string BranchDisplayName => DisplayText.Branch(MACN);
        public string StatusText => DisplayText.SoftDeleteStatus(TrangThaiXoa);
    }
}
