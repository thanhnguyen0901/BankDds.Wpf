using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels
{
    /// <summary>
    /// Handles HomeViewModel responsibilities in the application.
    /// </summary>
    public class HomeViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly IUserSession _userSession;
        private Func<Task>? _lastShowAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
        /// </summary>
        /// <param name="userSession"></param>
        public HomeViewModel(IUserSession userSession)
        {
            _userSession = userSession;
            base.DisplayName = "Trang chủ";
        }
        public ObservableCollection<string> Branches { get; } = new();
        public bool CanSwitchBranch => _userSession.UserGroup == UserGroup.NganHang;
        private string _selectedBranchCode = string.Empty;
        public string SelectedBranchCode
        {
            get => _selectedBranchCode;
            set
            {
                if (_selectedBranchCode == value) return;
                _selectedBranchCode = value;
                NotifyOfPropertyChange(() => SelectedBranchCode);
                if (!string.IsNullOrEmpty(value) && _userSession.PermittedBranches.Contains(value))
                {
                    _userSession.SetSelectedBranch(value);
                }
            }
        }
        public string Username => _userSession.Username;
        public string UserDisplayName => _userSession.DisplayName;
        public string SelectedBranch => _userSession.SelectedBranch;
        public string RoleText
        {
            get
            {
                return _userSession.UserGroup switch
                {
                    UserGroup.NganHang => "Ngân hàng (toàn hệ thống)",
                    UserGroup.ChiNhanh => $"Chi nhánh ({SelectedBranch})",
                    UserGroup.KhachHang => "Khách hàng",
                    _ => "Không xác định"
                };
            }
        }
        public bool CanViewCustomers => _userSession.UserGroup == UserGroup.ChiNhanh;
        public bool CanViewAccounts => _userSession.UserGroup == UserGroup.ChiNhanh;
        public bool CanViewEmployees => _userSession.UserGroup == UserGroup.ChiNhanh;
        public bool CanViewTransactions => _userSession.UserGroup == UserGroup.ChiNhanh;
        public bool CanViewReports => true;
        public bool CanViewAdmin => _userSession.UserGroup is UserGroup.NganHang or UserGroup.ChiNhanh;
        public bool CanViewBranches => _userSession.UserGroup == UserGroup.NganHang;
        public bool CanViewCustomerLookup => _userSession.UserGroup == UserGroup.NganHang;
        public bool IsCustomerMode => _userSession.UserGroup == UserGroup.KhachHang;

        protected override Task OnActivateAsync(CancellationToken cancellationToken)
        {
            Branches.Clear();
            foreach (var code in _userSession.PermittedBranches)
                Branches.Add(code);
            _selectedBranchCode = _userSession.SelectedBranch;
            NotifyOfPropertyChange(() => SelectedBranchCode);
            _userSession.SelectedBranchChanged += OnBranchChanged;
            RefreshPermissions();
            return base.OnActivateAsync(cancellationToken);
        }

        protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
        {
            _userSession.SelectedBranchChanged -= OnBranchChanged;
            return base.OnDeactivateAsync(close, cancellationToken);
        }

        private async void OnBranchChanged()
        {
            _selectedBranchCode = _userSession.SelectedBranch;
            NotifyOfPropertyChange(() => SelectedBranchCode);
            NotifyOfPropertyChange(() => SelectedBranch);
            NotifyOfPropertyChange(() => RoleText);
            if (_lastShowAction != null)
            {
                var action = _lastShowAction;
                await DeactivateItemAsync(ActiveItem, close: true, cancellationToken: default);
                await action();
            }
        }

        private void RefreshPermissions()
        {
            NotifyOfPropertyChange(() => Username);
            NotifyOfPropertyChange(() => UserDisplayName);
            NotifyOfPropertyChange(() => SelectedBranch);
            NotifyOfPropertyChange(() => RoleText);
            NotifyOfPropertyChange(() => CanViewCustomers);
            NotifyOfPropertyChange(() => CanViewAccounts);
            NotifyOfPropertyChange(() => CanViewEmployees);
            NotifyOfPropertyChange(() => CanViewTransactions);
            NotifyOfPropertyChange(() => CanViewReports);
            NotifyOfPropertyChange(() => CanViewAdmin);
            NotifyOfPropertyChange(() => CanViewBranches);
            NotifyOfPropertyChange(() => CanViewCustomerLookup);
            NotifyOfPropertyChange(() => CanSwitchBranch);
            NotifyOfPropertyChange(() => IsCustomerMode);
        }

        public async Task ShowCustomers()
        {
            if (!CanViewCustomers) return;
            _lastShowAction = ShowCustomers;
            var vm = IoC.Get<CustomersViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowAccounts()
        {
            if (!CanViewAccounts) return;
            _lastShowAction = ShowAccounts;
            var vm = IoC.Get<AccountsViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowEmployees()
        {
            if (!CanViewEmployees) return;
            _lastShowAction = ShowEmployees;
            var vm = IoC.Get<EmployeesViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowTransactions()
        {
            if (!CanViewTransactions) return;
            _lastShowAction = ShowTransactions;
            var vm = IoC.Get<TransactionsViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowReports()
        {
            _lastShowAction = ShowReports;
            var vm = IoC.Get<ReportsViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowAdmin()
        {
            if (!CanViewAdmin) return;
            _lastShowAction = ShowAdmin;
            var vm = IoC.Get<AdminViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowBranches()
        {
            if (!CanViewBranches) return;
            _lastShowAction = ShowBranches;
            var vm = IoC.Get<BranchesViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task ShowCustomerLookup()
        {
            if (!CanViewCustomerLookup) return;
            _lastShowAction = ShowCustomerLookup;
            var vm = IoC.Get<CustomerLookupViewModel>();
            await ActivateItemAsync(vm, cancellationToken: default);
        }

        public async Task Logout()
        {
            _lastShowAction = null;
            _userSession.ClearSession();
            if (Parent is MainShellViewModel mainShell)
            {
                await mainShell.ShowLoginAsync();
            }
        }
    }
}
