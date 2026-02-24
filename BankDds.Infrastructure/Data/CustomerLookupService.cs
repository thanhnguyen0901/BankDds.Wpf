using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

/// <summary>
/// Service layer for cross-branch customer lookup.
/// Enforces that only NGANHANG-role users may call the lookup repository.
/// </summary>
public class CustomerLookupService : ICustomerLookupService
{
    private readonly ICustomerLookupRepository _customerLookupRepository;
    private readonly IAuthorizationService _authorizationService;

    public CustomerLookupService(
        ICustomerLookupRepository customerLookupRepository,
        IAuthorizationService authorizationService)
    {
        _customerLookupRepository = customerLookupRepository;
        _authorizationService = authorizationService;
    }

    /// <inheritdoc />
    public Task<Customer?> GetCustomerByCmndAsync(string cmnd)
    {
        RequireNganHang();
        return _customerLookupRepository.GetCustomerByCmndAsync(cmnd);
    }

    /// <inheritdoc />
    public Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50)
    {
        RequireNganHang();
        return _customerLookupRepository.SearchCustomersByNameAsync(keyword, maxResults);
    }

    /// <summary>
    /// Customer lookup is a bank-level feature. Chi nhánh and Khách hàng roles
    /// should use the normal branch-scoped customer views instead.
    /// </summary>
    private void RequireNganHang()
    {
        if (!_authorizationService.CanAccessBranch("ALL"))
            throw new UnauthorizedAccessException(
                "Cross-branch customer lookup is restricted to bank-level users.");
    }
}
