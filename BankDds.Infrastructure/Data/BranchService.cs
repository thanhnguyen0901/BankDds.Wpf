using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    public class BranchService : IBranchService
    {
        private readonly IBranchRepository _branchRepository;
        private readonly IAuthorizationService _authorizationService;
        public BranchService(
            IBranchRepository branchRepository,
            IAuthorizationService authorizationService)
        {
            _branchRepository = branchRepository;
            _authorizationService = authorizationService;
        }

        public Task<List<Branch>> GetAllBranchesAsync() =>
            _branchRepository.GetAllBranchesAsync();

        public Task<Branch?> GetBranchAsync(string macn) =>
            _branchRepository.GetBranchAsync(macn);

        public Task<bool> BranchExistsAsync(string macn) =>
            _branchRepository.BranchExistsAsync(macn);

        public Task<bool> AddBranchAsync(Branch branch)
        {
            if (!_authorizationService.CanAccessBranch("ALL"))
                throw new UnauthorizedAccessException("Chỉ quản trị viên nhóm NganHang mới được thêm chi nhánh.");
            return _branchRepository.AddBranchAsync(branch);
        }

        public Task<bool> UpdateBranchAsync(Branch branch)
        {
            if (!_authorizationService.CanAccessBranch("ALL"))
                throw new UnauthorizedAccessException("Chỉ quản trị viên nhóm NganHang mới được cập nhật chi nhánh.");
            return _branchRepository.UpdateBranchAsync(branch);
        }

        public Task<bool> DeleteBranchAsync(string macn)
        {
            if (!_authorizationService.CanAccessBranch("ALL"))
                throw new UnauthorizedAccessException("Chỉ quản trị viên nhóm NganHang mới được xóa chi nhánh.");
            return _branchRepository.DeleteBranchAsync(macn);
        }
    }
}