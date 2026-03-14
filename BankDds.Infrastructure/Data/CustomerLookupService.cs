using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Provides cross-branch customer lookup features for bank-level users.
    /// </summary>
    public class CustomerLookupService : ICustomerLookupService
    {
        private readonly ICustomerLookupRepository _customerLookupRepository;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Initializes lookup service with lookup repository and authorization checks.
        /// </summary>
        /// <param name="customerLookupRepository">Customer lookup repository.</param>
        /// <param name="authorizationService">Authorization service for role and branch checks.</param>
        public CustomerLookupService(
            ICustomerLookupRepository customerLookupRepository,
            IAuthorizationService authorizationService)
        {
            _customerLookupRepository = customerLookupRepository;
            _authorizationService = authorizationService;
        }

        public Task<Customer?> GetCustomerByCmndAsync(string cmnd)
        {
            RequireNganHang();
            return _customerLookupRepository.GetCustomerByCmndAsync(cmnd);
        }

        public Task<List<Customer>> SearchCustomersByNameAsync(string keyword, int maxResults = 50)
        {
            RequireNganHang();
            return _customerLookupRepository.SearchCustomersByNameAsync(keyword, maxResults);
        }

        private void RequireNganHang()
        {
            // Logic: global customer lookup spans multiple shards and is allowed only for NganHang role.
            if (!_authorizationService.CanAccessBranch("ALL"))
            {
                throw new UnauthorizedAccessException(
                    "Tra cứu khách hàng liên chi nhánh chỉ dành cho người dùng NganHang.");
            }
        }
    }
}
