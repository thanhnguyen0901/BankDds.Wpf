using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a customer profile that owns accounts and is registered at one home branch.
    /// </summary>
    public class Customer : ObservableModel
    {
        private string _cmnd = string.Empty;
        private string _ho = string.Empty;
        private string _ten = string.Empty;
        private DateTime? _ngaySinh;
        private string _diaChi = string.Empty;
        private DateTime? _ngayCap;
        private string _sodt = string.Empty;
        private string _phai = string.Empty;
        private string _maCN = string.Empty;
        private int _trangThaiXoa;

        public string CMND
        {
            get => _cmnd;
            set => SetProperty(ref _cmnd, value);
        }

        public string Ho
        {
            get => _ho;
            set => SetProperty(ref _ho, value, nameof(FullName));
        }

        public string Ten
        {
            get => _ten;
            set => SetProperty(ref _ten, value, nameof(FullName));
        }

        public DateTime? NgaySinh
        {
            get => _ngaySinh;
            set => SetProperty(ref _ngaySinh, value);
        }

        public string DiaChi
        {
            get => _diaChi;
            set => SetProperty(ref _diaChi, value);
        }

        public DateTime? NgayCap
        {
            get => _ngayCap;
            set => SetProperty(ref _ngayCap, value);
        }

        public string SODT
        {
            get => _sodt;
            set => SetProperty(ref _sodt, value, nameof(SDT));
        }

        public string SDT
        {
            get => SODT;
            set => SODT = value;
        }

        public string Phai
        {
            get => _phai;
            set => SetProperty(ref _phai, value);
        }

        public string MaCN
        {
            get => _maCN;
            set => SetProperty(ref _maCN, value, nameof(BranchDisplayName));
        }

        public int TrangThaiXoa
        {
            get => _trangThaiXoa;
            set => SetProperty(ref _trangThaiXoa, value, nameof(StatusText));
        }

        public string FullName => $"{Ho} {Ten}";
        public string BranchDisplayName => DisplayText.Branch(MaCN);
        public string StatusText => DisplayText.SoftDeleteStatus(TrangThaiXoa);
    }
}
