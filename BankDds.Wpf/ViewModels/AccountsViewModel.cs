using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;
using System.Linq;

namespace BankDds.Wpf.ViewModels
{
    /// <summary>
    /// Provides a branch-scoped, read-only account lookup screen for ChiNhanh users.
    /// </summary>
    public class AccountsViewModel : BaseViewModel
    {
        private readonly IAccountService _accountService;
        private readonly IUserSession _userSession;
        private readonly List<Account> _allAccounts = new();
        private ObservableCollection<Account> _accounts = new();
        private Account? _selectedAccount;
        private string _searchText = string.Empty;
        private string _resultSummary = "Nhập số tài khoản hoặc CMND để tra cứu trong chi nhánh hiện tại.";

        public AccountsViewModel(IAccountService accountService, IUserSession userSession)
        {
            _accountService = accountService;
            _userSession = userSession;
            DisplayName = "Tra cứu tài khoản";
        }

        public ObservableCollection<Account> Accounts
        {
            get => _accounts;
            set
            {
                _accounts = value;
                NotifyOfPropertyChange(() => Accounts);
            }
        }

        public Account? SelectedAccount
        {
            get => _selectedAccount;
            set
            {
                _selectedAccount = value;
                NotifyOfPropertyChange(() => SelectedAccount);
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                NotifyOfPropertyChange(() => SearchText);
                NotifyOfPropertyChange(() => CanSearch);
            }
        }

        public string ResultSummary
        {
            get => _resultSummary;
            set
            {
                _resultSummary = value;
                NotifyOfPropertyChange(() => ResultSummary);
            }
        }

        public string CurrentBranchLabel => _userSession.SelectedBranch;
        public bool CanSearch => !IsLoading;

        protected override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            await base.OnActivateAsync(cancellationToken);
            await LoadAccountsAsync();
        }

        public Task Refresh() => LoadAccountsAsync();

        public Task Search()
        {
            ApplyFilter();
            return Task.CompletedTask;
        }

        private async Task LoadAccountsAsync()
        {
            await ExecuteWithLoadingAsync(async () =>
            {
                var accounts = _userSession.UserGroup == UserGroup.NganHang
                    ? await _accountService.GetAllAccountsAsync()
                    : await _accountService.GetAccountsByBranchAsync(_userSession.SelectedBranch);

                _allAccounts.Clear();
                _allAccounts.AddRange(accounts);
                ApplyFilter();
            });
        }

        private void ApplyFilter()
        {
            IEnumerable<Account> filtered = _allAccounts;
            var keyword = SearchText?.Trim();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                filtered = filtered.Where(account =>
                    account.SOTK.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    account.CMND.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            }

            var result = filtered
                .OrderBy(account => account.SOTK, StringComparer.OrdinalIgnoreCase)
                .ToList();

            Accounts = new ObservableCollection<Account>(result);
            ResultSummary = result.Count == 0
                ? "Không tìm thấy tài khoản phù hợp trong phạm vi tra cứu hiện tại."
                : $"Tìm thấy {result.Count} tài khoản trong phạm vi tra cứu hiện tại.";
        }
    }
}
