using BankDds.Core.Formatting;

namespace BankDds.Core.Models
{
    /// <summary>
    /// Represents an application login mapped to role, branch scope, and person identity.
    /// </summary>
    public class User : ObservableModel
    {
        private string _username = string.Empty;
        private string _passwordHash = string.Empty;
        private UserGroup _userGroup;
        private string _defaultBranch = string.Empty;
        private string? _customerCMND;
        private string? _employeeId;
        private int _trangThaiXoa;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set => SetProperty(ref _passwordHash, value);
        }

        public UserGroup UserGroup
        {
            get => _userGroup;
            set => SetProperty(ref _userGroup, value);
        }

        public string DefaultBranch
        {
            get => _defaultBranch;
            set => SetProperty(ref _defaultBranch, value, nameof(DefaultBranchDisplayName));
        }

        public string? CustomerCMND
        {
            get => _customerCMND;
            set => SetProperty(ref _customerCMND, value);
        }

        public string? EmployeeId
        {
            get => _employeeId;
            set => SetProperty(ref _employeeId, value);
        }

        public int TrangThaiXoa
        {
            get => _trangThaiXoa;
            set => SetProperty(ref _trangThaiXoa, value, nameof(StatusText));
        }

        public string DefaultBranchDisplayName => DisplayText.Branch(DefaultBranch);
        public string StatusText => DisplayText.SoftDeleteStatus(TrangThaiXoa);
    }
}
