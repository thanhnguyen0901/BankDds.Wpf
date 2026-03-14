using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuthorizationService _authorizationService;

        public CustomerService(ICustomerRepository customerRepository, IAuthorizationService authorizationService)
        {
            _customerRepository = customerRepository;
            _authorizationService = authorizationService;
        }

        public Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
            return _customerRepository.GetCustomersByBranchAsync(branchCode);
        }

        public Task<List<Customer>> GetAllCustomersAsync()
        {
            if (!_authorizationService.CanAccessBranch("ALL"))
            {
                throw new UnauthorizedAccessException("Chỉ người dùng NganHang mới được xem toàn bộ khách hàng.");
            }
            return _customerRepository.GetAllCustomersAsync();
        }

        public async Task<Customer?> GetCustomerByCMNDAsync(string cmnd)
        {
            var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
            if (customer == null)
                return null;
            _authorizationService.RequireCanAccessCustomer(cmnd);
            _authorizationService.RequireCanAccessBranch(customer.MaCN);
            return customer;
        }

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            _authorizationService.RequireCanModifyBranch(customer.MaCN);
            return await _customerRepository.AddCustomerAsync(customer);
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var existing = await _customerRepository.GetCustomerByCMNDAsync(customer.CMND);
            if (existing == null)
                return false;
            _authorizationService.RequireCanModifyBranch(existing.MaCN);
            _authorizationService.RequireCanModifyBranch(customer.MaCN);
            return await _customerRepository.UpdateCustomerAsync(customer);
        }

        public async Task<bool> DeleteCustomerAsync(string cmnd)
        {
            var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
            if (customer == null)
                return false;
            _authorizationService.RequireCanModifyBranch(customer.MaCN);
            return await _customerRepository.DeleteCustomerAsync(cmnd);
        }

        public async Task<bool> RestoreCustomerAsync(string cmnd)
        {
            var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
            if (customer == null)
                return false;
            _authorizationService.RequireCanModifyBranch(customer.MaCN);
            return await _customerRepository.RestoreCustomerAsync(cmnd);
        }
    }
}