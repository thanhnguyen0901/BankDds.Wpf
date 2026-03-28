using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a branch employee who performs operations and transactions in the system.
    /// </summary>
    public class Employee : ObservableModel
    {
        private string _manv = string.Empty;
        private string _ho = string.Empty;
        private string _ten = string.Empty;
        private string _diaChi = string.Empty;
        private string _cmnd = string.Empty;
        private string _phai = string.Empty;
        private string _sodt = string.Empty;
        private string _macn = string.Empty;
        private int _trangThaiXoa;

        public string MANV
        {
            get => _manv;
            set => SetProperty(ref _manv, value);
        }

        public string HO
        {
            get => _ho;
            set => SetProperty(ref _ho, value, nameof(FullName));
        }

        public string TEN
        {
            get => _ten;
            set => SetProperty(ref _ten, value, nameof(FullName));
        }

        public string DIACHI
        {
            get => _diaChi;
            set => SetProperty(ref _diaChi, value);
        }

        public string CMND
        {
            get => _cmnd;
            set => SetProperty(ref _cmnd, value);
        }

        public string PHAI
        {
            get => _phai;
            set => SetProperty(ref _phai, value);
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

        public string MACN
        {
            get => _macn;
            set => SetProperty(ref _macn, value, nameof(BranchDisplayName));
        }

        public int TrangThaiXoa
        {
            get => _trangThaiXoa;
            set => SetProperty(ref _trangThaiXoa, value, nameof(StatusText));
        }

        public string FullName => $"{HO} {TEN}";
        public string BranchDisplayName => DisplayText.Branch(MACN);
        public string StatusText => DisplayText.SoftDeleteStatus(TrangThaiXoa);
    }
}
