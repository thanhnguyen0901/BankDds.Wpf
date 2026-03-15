using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Provides reporting use cases with role-based scope validation.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserSession _userSession;

        /// <summary>
        /// Initializes report service with report repository, account repository, authorization, and session context.
        /// </summary>
        /// <param name="reportRepository">Report data repository.</param>
        /// <param name="accountRepository">Account data repository.</param>
        /// <param name="authorizationService">Authorization service for role and branch checks.</param>
        /// <param name="userSession">Current authenticated user session.</param>
        public ReportService(
            IReportRepository reportRepository,
            IAccountRepository accountRepository,
            IAuthorizationService authorizationService,
            IUserSession userSession)
        {
            _reportRepository = reportRepository;
            _accountRepository = accountRepository;
            _authorizationService = authorizationService;
            _userSession = userSession;
        }

        public async Task<AccountStatement> GetAccountStatementAsync(string sotk, DateTime fromDate, DateTime toDate)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);

            if (account == null)
            {
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            }

            _authorizationService.RequireCanAccessAccount(account.CMND);

            // Logic: only KhachHang can view own accounts cross-branch; staff roles remain branch-scoped.
            if (_userSession.UserGroup != UserGroup.KhachHang)
            {
                _authorizationService.RequireCanAccessBranch(account.MACN);
            }

            var statement = await _reportRepository.GetAccountStatementAsync(
                sotk,
                fromDate,
                toDate,
                _userSession.UserGroup == UserGroup.KhachHang ? _userSession.CustomerCMND : null);

            if (statement == null)
            {
                throw new InvalidOperationException("Không thể tạo sao kê tài khoản.");
            }

            return statement;
        }

        public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
        {
            _authorizationService.RequireCanAccessReports();

            if (!string.IsNullOrEmpty(branchCode) && branchCode != "ALL")
            {
                _authorizationService.RequireCanAccessBranch(branchCode);
            }

            // Logic: when no explicit branch is selected, apply effective branch filter from role/session.
            var effectiveBranch = (!string.IsNullOrEmpty(branchCode) && branchCode != "ALL")
                ? branchCode
                : _authorizationService.GetEffectiveBranchFilter();

            return await _reportRepository.GetAccountsOpenedInPeriodAsync(fromDate, toDate, effectiveBranch);
        }

        public async Task<Dictionary<string, int>> GetCustomerCountByBranchAsync()
        {
            _authorizationService.RequireCanAccessReports();

            var branchFilter = _authorizationService.GetEffectiveBranchFilter();
            var customers = await _reportRepository.GetCustomersByBranchAsync(branchFilter);

            return customers
                .GroupBy(c => c.MaCN)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public async Task<List<Customer>> GetCustomersByBranchReportAsync(string? branchCode = null)
        {
            _authorizationService.RequireCanAccessReports(branchCode);

            if (branchCode != null && branchCode != "ALL")
            {
                _authorizationService.RequireCanAccessBranch(branchCode);
            }

            return await _reportRepository.GetCustomersByBranchAsync(branchCode);
        }

        public async Task<TransactionSummary> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
        {
            _authorizationService.RequireCanAccessReports(branchCode);

            if (branchCode != null && branchCode != "ALL")
            {
                _authorizationService.RequireCanAccessBranch(branchCode);
            }

            var summary = await _reportRepository.GetTransactionSummaryAsync(fromDate, toDate, branchCode);

            if (summary == null)
            {
                throw new InvalidOperationException("Không thể tạo báo cáo tổng hợp giao dịch.");
            }

            return summary;
        }
    }
}
