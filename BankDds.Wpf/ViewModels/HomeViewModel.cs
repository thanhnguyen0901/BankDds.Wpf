using Caliburn.Micro;
using BankDds.Core.Formatting;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Wpf.ViewModels
{
    /// <summary>
    /// Coordinates main menu navigation and feature visibility based on the logged-in user role.
    /// </summary>
    public class HomeViewModel : Conductor<Screen>.Collection.OneActive
    {
        private readonly IUserSession _userSession;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeViewModel"/> class.
        /// </summary>
        /// <param name="userSession">Current authenticated session with role, branch, and identity context.</param>
        public HomeViewModel(IUserSession userSession)
        {
            _userSession = userSession;
            base.DisplayName = "Trang chủ";
        }

        public string Username => _userSession.Username;
        public string UserDisplayName => _userSession.DisplayName;
        public string SelectedBranch => _userSession.SelectedBranch;

        public string RoleText => _userSession.UserGroup switch
        {
            UserGroup.NganHang => "Ngân hàng (toàn hệ thống)",
            UserGroup.ChiNhanh => $"Chi nhánh ({DisplayText.Branch(SelectedBranch)})",
            UserGroup.KhachHang => "Khách hàng",
            _ => "Không xác định"
        };

        public string AccountsTabText => "Tra cứu tài khoản";

        public string AdminTabText => _userSession.UserGroup == UserGroup.NganHang
            ? "Quản trị"
            : "Tạo người dùng";

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
            RefreshPermissions();
            return base.OnActivateAsync(cancellationToken);
        }

        private void RefreshPermissions()
        {
            NotifyOfPropertyChange(() => Username);
            NotifyOfPropertyChange(() => UserDisplayName);
            NotifyOfPropertyChange(() => SelectedBranch);
            NotifyOfPropertyChange(() => RoleText);
            NotifyOfPropertyChange(() => AccountsTabText);
            NotifyOfPropertyChange(() => AdminTabText);
            NotifyOfPropertyChange(() => CanViewCustomers);
            NotifyOfPropertyChange(() => CanViewAccounts);
            NotifyOfPropertyChange(() => CanViewEmployees);
            NotifyOfPropertyChange(() => CanViewTransactions);
            NotifyOfPropertyChange(() => CanViewReports);
            NotifyOfPropertyChange(() => CanViewAdmin);
            NotifyOfPropertyChange(() => CanViewBranches);
            NotifyOfPropertyChange(() => CanViewCustomerLookup);
            NotifyOfPropertyChange(() => IsCustomerMode);
        }

        public async Task ShowCustomers()
        {
            if (!CanViewCustomers) return;
            await ActivateItemAsync(IoC.Get<CustomersViewModel>(), cancellationToken: default);
        }

        public async Task ShowAccounts()
        {
            if (!CanViewAccounts) return;
            await ActivateItemAsync(IoC.Get<AccountsViewModel>(), cancellationToken: default);
        }

        public async Task ShowEmployees()
        {
            if (!CanViewEmployees) return;
            await ActivateItemAsync(IoC.Get<EmployeesViewModel>(), cancellationToken: default);
        }

        public async Task ShowTransactions()
        {
            if (!CanViewTransactions) return;
            await ActivateItemAsync(IoC.Get<TransactionsViewModel>(), cancellationToken: default);
        }

        public async Task ShowReports() =>
            await ActivateItemAsync(IoC.Get<ReportsViewModel>(), cancellationToken: default);

        public async Task ShowAdmin()
        {
            if (!CanViewAdmin) return;
            await ActivateItemAsync(IoC.Get<AdminViewModel>(), cancellationToken: default);
        }

        public async Task ShowBranches()
        {
            if (!CanViewBranches) return;
            await ActivateItemAsync(IoC.Get<BranchesViewModel>(), cancellationToken: default);
        }

        public async Task ShowCustomerLookup()
        {
            if (!CanViewCustomerLookup) return;
            await ActivateItemAsync(IoC.Get<CustomerLookupViewModel>(), cancellationToken: default);
        }

        public async Task Logout()
        {
            _userSession.ClearSession();
            if (Parent is MainShellViewModel mainShell)
            {
                await mainShell.ShowLoginAsync();
            }
        }
    }
}
