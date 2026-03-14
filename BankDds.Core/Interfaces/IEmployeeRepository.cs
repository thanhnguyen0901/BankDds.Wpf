using BankDds.Core.Models;

namespace BankDds.Core.Interfaces
{
    /// <summary>
    /// Defines persistence operations for employee records and branch transfer.
    /// </summary>
    public interface IEmployeeRepository
    {
        /// <summary>
        /// Gets employees that belong to a specific branch.
        /// </summary>
        /// <param name="branchCode">Branch code.</param>
        /// <returns>A list of employees that belong to the requested branch.</returns>
        Task<List<Employee>> GetEmployeesByBranchAsync(string branchCode);

        /// <summary>
        /// Gets all employees visible to current scope.
        /// </summary>
        /// <returns>A list of employees available in the current data scope.</returns>
        Task<List<Employee>> GetAllEmployeesAsync();

        /// <summary>
        /// Gets employee details by employee code.
        /// </summary>
        /// <param name="manv">Employee code.</param>
        /// <returns>The employee details when found; otherwise null.</returns>
        Task<Employee?> GetEmployeeAsync(string manv);

        /// <summary>
        /// Creates a new employee record.
        /// </summary>
        /// <param name="employee">Employee entity.</param>
        /// <returns>True when an employee record is created successfully; otherwise false.</returns>
        Task<bool> AddEmployeeAsync(Employee employee);

        /// <summary>
        /// Updates employee information.
        /// </summary>
        /// <param name="employee">Employee entity.</param>
        /// <returns>True when employee information is updated successfully; otherwise false.</returns>
        Task<bool> UpdateEmployeeAsync(Employee employee);

        /// <summary>
        /// Soft-deletes an employee record.
        /// </summary>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when the employee is marked as deleted successfully; otherwise false.</returns>
        Task<bool> DeleteEmployeeAsync(string manv);

        /// <summary>
        /// Restores a previously deleted employee record.
        /// </summary>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when a deleted employee is restored successfully; otherwise false.</returns>
        Task<bool> RestoreEmployeeAsync(string manv);

        /// <summary>
        /// Transfers an employee to another branch.
        /// </summary>
        /// <param name="manv">Employee code.</param>
        /// <param name="newBranch">Destination branch code.</param>
        /// <returns>True when the employee is transferred to another branch successfully; otherwise false.</returns>
        Task<bool> TransferEmployeeAsync(string manv, string newBranch);

        /// <summary>
        /// Generates next employee identifier.
        /// </summary>
        /// <returns>The next generated employee code for new records.</returns>
        Task<string> GenerateEmployeeIdAsync();

        /// <summary>
        /// Determines whether the specified employee code already exists.
        /// </summary>
        /// <param name="manv">Employee code.</param>
        /// <returns>True when the specified employee code exists; otherwise false.</returns>
        Task<bool> EmployeeExistsAsync(string manv);

    }
}
