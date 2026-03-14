using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserSession _userSession;
        public AccountService(
            IAccountRepository accountRepository,
            ICustomerRepository customerRepository,
            IAuthorizationService authorizationService,
            IUserSession userSession)
        {
            _accountRepository = accountRepository;
            _customerRepository = customerRepository;
            _authorizationService = authorizationService;
            _userSession = userSession;
        }

        public Task<List<Account>> GetAccountsByBranchAsync(string branchCode)
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
            return _accountRepository.GetAccountsByBranchAsync(branchCode);
        }

        public Task<List<Account>> GetAllAccountsAsync()
        {
            if (!_authorizationService.CanAccessBranch("ALL"))
            {
                throw new UnauthorizedAccessException("Chỉ người dùng NganHang mới được truy cập toàn bộ tài khoản.");
            }
            return _accountRepository.GetAllAccountsAsync();
        }

        public async Task<List<Account>> GetAccountsByCustomerAsync(string cmnd)
        {
            _authorizationService.RequireCanAccessCustomer(cmnd);
            var customer = await _customerRepository.GetCustomerByCMNDAsync(cmnd);
            if (customer != null)
            {
                _authorizationService.RequireCanAccessBranch(customer.MaCN);
            }
            return await _accountRepository.GetAccountsByCustomerAsync(cmnd);
        }

        public async Task<Account?> GetAccountAsync(string sotk)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return null;
            _authorizationService.RequireCanAccessAccount(account.CMND);
            if (_userSession.UserGroup != UserGroup.KhachHang)
            {
                _authorizationService.RequireCanAccessBranch(account.MACN);
            }
            return account;
        }

        public async Task<bool> AddAccountAsync(Account account)
        {
            _authorizationService.RequireCanModifyBranch(account.MACN);
            var customer = await _customerRepository.GetCustomerByCMNDAsync(account.CMND);
            if (customer == null)
                throw new InvalidOperationException("Không tìm thấy khách hàng.");
            _authorizationService.RequireCanAccessCustomer(account.CMND);
            return await _accountRepository.AddAccountAsync(account);
        }

        public async Task<bool> UpdateAccountAsync(Account account)
        {
            var existing = await _accountRepository.GetAccountAsync(account.SOTK);
            if (existing == null)
                return false;
            _authorizationService.RequireCanModifyBranch(existing.MACN);
            return await _accountRepository.UpdateAccountAsync(account);
        }

        public async Task<bool> DeleteAccountAsync(string sotk)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return false;
            _authorizationService.RequireCanModifyBranch(account.MACN);
            return await _accountRepository.DeleteAccountAsync(sotk);
        }

        public async Task<bool> CloseAccountAsync(string sotk)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return false;
            _authorizationService.RequireCanModifyBranch(account.MACN);
            return await _accountRepository.CloseAccountAsync(sotk);
        }

        public async Task<bool> ReopenAccountAsync(string sotk)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return false;
            _authorizationService.RequireCanModifyBranch(account.MACN);
            return await _accountRepository.ReopenAccountAsync(sotk);
        }
    }
}