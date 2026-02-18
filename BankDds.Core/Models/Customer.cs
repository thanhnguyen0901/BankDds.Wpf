namespace BankDds.Core.Models;

public class Customer
{
    public string CMND { get; set; } = string.Empty;
    public string Ho { get; set; } = string.Empty;
    public string Ten { get; set; } = string.Empty;
    public string DiaChi { get; set; } = string.Empty;
    public string SDT { get; set; } = string.Empty;
    public string Phai { get; set; } = string.Empty;
    public string MaCN { get; set; } = string.Empty;
    public int TrangThaiXoa { get; set; } = 0; // 0 = Active, 1 = Deleted

    public string FullName => $"{Ho} {Ten}";
    public string StatusText => TrangThaiXoa == 0 ? "Active" : "Deleted";
}
