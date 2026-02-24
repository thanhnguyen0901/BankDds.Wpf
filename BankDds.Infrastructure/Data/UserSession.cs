using BankDds.Core.Interfaces;
using BankDds.Core.Models;

namespace BankDds.Infrastructure.Data;

public class UserSession : IUserSession
{
    public string Username { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public UserGroup UserGroup { get; private set; }
    public string SelectedBranch { get; private set; } = string.Empty;
    public List<string> PermittedBranches { get; private set; } = new();
    public string? CustomerCMND { get; private set; }
    public string? EmployeeId { get; private set; }
    public bool IsAuthenticated { get; private set; }

    /// <inheritdoc />
    public event Action? SelectedBranchChanged;

    public void SetSession(
        string username,
        string displayName,
        UserGroup userGroup,
        string selectedBranch,
        List<string> permittedBranches,
        string? customerCMND = null,
        string? employeeId = null)
    {
        Username = username;
        DisplayName = displayName;
        UserGroup = userGroup;
        SelectedBranch = selectedBranch;
        PermittedBranches = permittedBranches ?? new List<string>();
        CustomerCMND = customerCMND;
        EmployeeId = employeeId;
        IsAuthenticated = true;
    }

    /// <inheritdoc />
    public void SetSelectedBranch(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new ArgumentException("Branch code cannot be empty.", nameof(branchCode));

        if (!PermittedBranches.Contains(branchCode))
            throw new InvalidOperationException(
                $"Branch '{branchCode}' is not in the permitted list for this session.");

        if (SelectedBranch == branchCode)
            return; // No change — skip event.

        SelectedBranch = branchCode;
        SelectedBranchChanged?.Invoke();
    }

    public void ClearSession()
    {
        Username = string.Empty;
        DisplayName = string.Empty;
        UserGroup = UserGroup.KhachHang;
        SelectedBranch = string.Empty;
        PermittedBranches.Clear();
        CustomerCMND = null;
        EmployeeId = null;
        IsAuthenticated = false;
    }
}
