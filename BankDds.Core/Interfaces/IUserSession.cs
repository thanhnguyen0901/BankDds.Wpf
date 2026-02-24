using BankDds.Core.Models;

namespace BankDds.Core.Interfaces;

public interface IUserSession
{
    string Username { get; }
    string DisplayName { get; }
    UserGroup UserGroup { get; }
    string SelectedBranch { get; }
    List<string> PermittedBranches { get; }
    string? CustomerCMND { get; }
    string? EmployeeId { get; }
    
    void SetSession(
        string username,
        string displayName,
        UserGroup userGroup,
        string selectedBranch,
        List<string> permittedBranches,
        string? customerCMND = null,
        string? employeeId = null);
    
    void ClearSession();
    
    bool IsAuthenticated { get; }

    /// <summary>
    /// Raised when <see cref="SelectedBranch"/> is changed after login
    /// (e.g. NGANHANG user switches branch via the UI).
    /// </summary>
    event Action? SelectedBranchChanged;

    /// <summary>
    /// Change the active branch. Only succeeds when <paramref name="branchCode"/>
    /// is contained in <see cref="PermittedBranches"/>.
    /// </summary>
    void SetSelectedBranch(string branchCode);
}
