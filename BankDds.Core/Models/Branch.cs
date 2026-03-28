using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents a bank branch master record used for branch routing and ownership.
    /// </summary>
    public class Branch : ObservableModel
    {
        private string _macn = string.Empty;
        private string _tencn = string.Empty;
        private string _diaChi = string.Empty;
        private string _sodt = string.Empty;

        public string MACN
        {
            get => _macn;
            set => SetProperty(ref _macn, value, nameof(DisplayName));
        }

        public string TENCN
        {
            get => _tencn;
            set => SetProperty(ref _tencn, value, nameof(DisplayName));
        }

        public string DiaChi
        {
            get => _diaChi;
            set => SetProperty(ref _diaChi, value);
        }

        public string SODT
        {
            get => _sodt;
            set => SetProperty(ref _sodt, value, nameof(SoDT));
        }

        public string SoDT
        {
            get => SODT;
            set => SODT = value;
        }

        public string DisplayName => string.IsNullOrWhiteSpace(TENCN) ? DisplayText.Branch(MACN) : TENCN.Trim();
    }
}
