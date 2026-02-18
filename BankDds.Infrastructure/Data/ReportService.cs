using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// Report service that delegates to IReportRepository for data access with authorization
/// </summary>
public class ReportService : IReportService
{
    private readonly IReportRepository _reportRepository;
    private readonly IAccountRepository _accountRepository;
    private readonly IAuthorizationService _authorizationService;

    public ReportService(
        IReportRepository reportRepository,
        IAccountRepository accountRepository,
        IAuthorizationService authorizationService)
    {
        _reportRepository = reportRepository;
        _accountRepository = accountRepository;
        _authorizationService = authorizationService;
    }

    public async Task<AccountStatement> GetAccountStatementAsync(string sotk, DateTime fromDate, DateTime toDate)
    {
        var account = await _accountRepository.GetAccountAsync(sotk);
        if (account == null)
            throw new InvalidOperationException("Account not found");

        // Verify user can access this account
        _authorizationService.RequireCanAccessAccount(account.CMND);
        _authorizationService.RequireCanAccessBranch(account.MACN);
        
        var statement = await _reportRepository.GetAccountStatementAsync(sotk, fromDate, toDate);
        if (statement == null)
            throw new InvalidOperationException("Unable to generate account statement");
        
        return statement;
    }

    public async Task<List<Account>> GetAccountsOpenedInPeriodAsync(DateTime fromDate, DateTime toDate)
    {
        _authorizationService.RequireCanAccessReports();

        // NganHang gets all branches; ChiNhanh is automatically scoped to their branch
        var branchFilter = _authorizationService.GetEffectiveBranchFilter();
        return await _reportRepository.GetAccountsOpenedInPeriodAsync(fromDate, toDate, branchFilter);
    }

    public async Task<Dictionary<string, int>> GetCustomerCountByBranchAsync()
    {
        _authorizationService.RequireCanAccessReports();

        // NganHang sees all branches; ChiNhanh sees only their branch
        var branchFilter = _authorizationService.GetEffectiveBranchFilter();
        var customers = await _reportRepository.GetCustomersByBranchAsync(branchFilter);
        return customers
            .GroupBy(c => c.MaCN)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Customer>> GetCustomersByBranchReportAsync(string? branchCode = null)
    {
        _authorizationService.RequireCanAccessReports(branchCode);

        // Verify branch access
        if (branchCode != null && branchCode != "ALL")
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
        }
        
        return await _reportRepository.GetCustomersByBranchAsync(branchCode);
    }

    public async Task<TransactionSummary> GetTransactionSummaryAsync(DateTime fromDate, DateTime toDate, string? branchCode = null)
    {
        _authorizationService.RequireCanAccessReports(branchCode);

        // Verify branch access
        if (branchCode != null && branchCode != "ALL")
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
        }
        
        var summary = await _reportRepository.GetTransactionSummaryAsync(fromDate, toDate, branchCode);
        if (summary == null)
            throw new InvalidOperationException("Unable to generate transaction summary");
        
        return summary;
    }
}
