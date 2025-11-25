namespace BankDds.Core.Models;

public class Employee
{
    public int MANV { get; set; }
    public string HO { get; set; } = string.Empty;
    public string TEN { get; set; } = string.Empty;
    public string DIACHI { get; set; } = string.Empty;
    public string SDT { get; set; } = string.Empty;
    public string MACN { get; set; } = string.Empty;

    public string FullName => $"{HO} {TEN}";
}
