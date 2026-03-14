namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a bank branch master record used for branch routing and ownership.
    /// </summary>
    public class Branch
    {
        public string MACN { get; set; } = string.Empty;
        public string TENCN { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public string SODT { get; set; } = string.Empty;
    }
}
