using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
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
            if (!_authorizationService.CanAccessBranch("ALL"))
                throw new UnauthorizedAccessException(
                    "Cross-branch customer lookup is restricted to bank-level users.");
        }
    }
}