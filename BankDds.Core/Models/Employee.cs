namespace BankDds.Core.Models;

public class Employee
{
    public string MANV { get; set; } = string.Empty;
    public string HO { get; set; } = string.Empty;
    public string TEN { get; set; } = string.Empty;
    public string DIACHI { get; set; } = string.Empty;
    public string CMND { get; set; } = string.Empty;
    public string PHAI { get; set; } = string.Empty; // "Nam" or "Nu"
    public string SDT { get; set; } = string.Empty;
    public string MACN { get; set; } = string.Empty;
    public int TrangThaiXoa { get; set; } = 0; // 0 = Active, 1 = Deleted

    public string FullName => $"{HO} {TEN}";
    public string StatusText => TrangThaiXoa == 0 ? "Active" : "Deleted";
}
