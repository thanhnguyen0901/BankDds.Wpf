using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data
{
    /// <summary>
    /// Executes employee management use cases with branch-scoped authorization.
    /// </summary>
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;
        private readonly IAuthorizationService _authorizationService;

        /// <summary>
        /// Initializes employee service with repository and authorization services.
        /// </summary>
        /// <param name="employeeRepository">Employee data repository.</param>
        /// <param name="authorizationService">Authorization service for role and branch checks.</param>
        public EmployeeService(
            IEmployeeRepository employeeRepository,
            IAuthorizationService authorizationService)
        {
            _employeeRepository = employeeRepository;
            _authorizationService = authorizationService;
        }

        public Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode)
        {
            _authorizationService.RequireCanAccessBranch(branchCode);
            return _employeeRepository.GetEmployeesByBranchAsync(branchCode);
        }

        public Task<List<Employee>> GetAllEmployeesAsync()
        {
            // Logic: full employee list across branches is restricted to NganHang.
            if (!_authorizationService.CanAccessBranch("ALL"))
            {
                throw new UnauthorizedAccessException("Chỉ người dùng NganHang mới được xem toàn bộ nhân viên.");
            }

            return _employeeRepository.GetAllEmployeesAsync();
        }

        public async Task<Employee?> GetEmployeeAsync(string manv)
        {
            var employee = await _employeeRepository.GetEmployeeAsync(manv);

            if (employee == null)
            {
                return null;
            }

            _authorizationService.RequireCanAccessBranch(employee.MACN);
            return employee;
        }

        public async Task<bool> AddEmployeeAsync(Employee employee)
        {
            _authorizationService.RequireCanModifyBranch(employee.MACN);
            return await _employeeRepository.AddEmployeeAsync(employee);
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            var existing = await _employeeRepository.GetEmployeeAsync(employee.MANV);

            if (existing == null)
            {
                return false;
            }

            _authorizationService.RequireCanModifyBranch(existing.MACN);
            _authorizationService.RequireCanModifyBranch(employee.MACN);
            return await _employeeRepository.UpdateEmployeeAsync(employee);
        }

        public async Task<bool> DeleteEmployeeAsync(string manv)
        {
            var employee = await _employeeRepository.GetEmployeeAsync(manv);

            if (employee == null)
            {
                return false;
            }

            _authorizationService.RequireCanModifyBranch(employee.MACN);
            return await _employeeRepository.DeleteEmployeeAsync(manv);
        }

        public async Task<bool> RestoreEmployeeAsync(string manv)
        {
            var employee = await _employeeRepository.GetEmployeeAsync(manv);

            if (employee == null)
            {
                return false;
            }

            _authorizationService.RequireCanModifyBranch(employee.MACN);
            return await _employeeRepository.RestoreEmployeeAsync(manv);
        }

        public async Task<bool> TransferEmployeeAsync(string manv, string newBranch)
        {
            // Logic: transfer requires modify permission in both source and target branch.
            var employee = await _employeeRepository.GetEmployeeAsync(manv);

            if (employee == null)
            {
                return false;
            }

            _authorizationService.RequireCanModifyBranch(employee.MACN);
            _authorizationService.RequireCanModifyBranch(newBranch);
            return await _employeeRepository.TransferEmployeeAsync(manv, newBranch);
        }

        public Task<string> GenerateEmployeeIdAsync() =>
            _employeeRepository.GenerateEmployeeIdAsync();

        public Task<bool> EmployeeExistsAsync(string manv) =>
            _employeeRepository.EmployeeExistsAsync(manv);
    }
}
