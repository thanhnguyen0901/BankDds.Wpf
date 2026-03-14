using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    public class TransactionService : ITransactionService
    {
        private readonly ITransactionRepository _transactionRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserSession _userSession;
        public TransactionService(
            ITransactionRepository transactionRepository,
            IAccountRepository accountRepository,
            IAuthorizationService authorizationService,
            IUserSession userSession)
        {
            _transactionRepository = transactionRepository;
            _accountRepository = accountRepository;
            _authorizationService = authorizationService;
            _userSession = userSession;
        }

        public async Task<List<Transaction>> GetTransactionsByAccountAsync(string sotk)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            _authorizationService.RequireCanAccessAccount(account.CMND);
            if (_userSession.UserGroup != UserGroup.KhachHang)
            {
                _authorizationService.RequireCanAccessBranch(account.MACN);
            }
            return await _transactionRepository.GetTransactionsByAccountAsync(sotk);
        }

        public Task<List<Transaction>> GetTransactionsByBranchAsync(string branchCode, DateTime? fromDate = null, DateTime? toDate = null)
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
            return _transactionRepository.GetTransactionsByBranchAsync(branchCode, fromDate, toDate);
        }

        public async Task<decimal> GetDailyWithdrawalTotalAsync(string accountNumber, DateTime date)
        {
            var account = await _accountRepository.GetAccountAsync(accountNumber);
            if (account == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            _authorizationService.RequireCanAccessAccount(account.CMND);
            return await _transactionRepository.GetDailyWithdrawalTotalAsync(accountNumber, date);
        }

        public async Task<decimal> GetDailyTransferTotalAsync(string accountNumber, DateTime date)
        {
            var account = await _accountRepository.GetAccountAsync(accountNumber);
            if (account == null)
                throw new InvalidOperationException("Không tìm thấy tài khoản.");
            _authorizationService.RequireCanAccessAccount(account.CMND);
            return await _transactionRepository.GetDailyTransferTotalAsync(accountNumber, date);
        }

        public async Task<bool> DepositAsync(string sotk, decimal amount, string manv)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return false;
            _authorizationService.RequireCanPerformTransactions(account.MACN);
            return await _transactionRepository.DepositAsync(sotk, amount, manv);
        }

        public async Task<bool> WithdrawAsync(string sotk, decimal amount, string manv)
        {
            var account = await _accountRepository.GetAccountAsync(sotk);
            if (account == null)
                return false;
            _authorizationService.RequireCanPerformTransactions(account.MACN);
            return await _transactionRepository.WithdrawAsync(sotk, amount, manv);
        }

        public async Task<bool> TransferAsync(string sotkFrom, string sotkTo, decimal amount, string manv)
        {
            var accountFrom = await _accountRepository.GetAccountAsync(sotkFrom);
            if (accountFrom == null)
                throw new InvalidOperationException($"Không tìm thấy tài khoản nguồn '{sotkFrom}'.");
            var accountTo = await _accountRepository.GetAccountAsync(sotkTo);
            if (accountTo == null)
                throw new InvalidOperationException($"Không tìm thấy tài khoản đích '{sotkTo}'.");
            _authorizationService.RequireCanPerformTransactions(accountFrom.MACN);
            if (accountFrom.MACN != accountTo.MACN)
            {
                _authorizationService.RequireCanAccessBranch(accountTo.MACN);
            }
            return await _transactionRepository.TransferAsync(sotkFrom, sotkTo, amount, manv);
        }
    }
}