using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a customer profile that owns accounts and is registered at one home branch.
    /// </summary>
    public class Customer
    {
        public string CMND { get; set; } = string.Empty;
        public string Ho { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public DateTime? NgaySinh { get; set; }
        public string DiaChi { get; set; } = string.Empty;
        public DateTime? NgayCap { get; set; }
        public string SODT { get; set; } = string.Empty;
        public string SDT
        {
            get => SODT;
            set => SODT = value;
        }
        public string Phai { get; set; } = string.Empty;
        public string MaCN { get; set; } = string.Empty;
        public int TrangThaiXoa { get; set; } = 0;
        public string FullName => $"{Ho} {Ten}";
        public string BranchDisplayName => DisplayText.Branch(MaCN);
        public string StatusText => DisplayText.SoftDeleteStatus(TrangThaiXoa);
    }
}
