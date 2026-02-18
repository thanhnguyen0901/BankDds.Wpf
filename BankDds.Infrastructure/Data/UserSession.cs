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
    public int? EmployeeId { get; private set; }
    public bool IsAuthenticated { get; private set; }

    public void SetSession(
        string username,
        string displayName,
        UserGroup userGroup,
        string selectedBranch,
        List<string> permittedBranches,
        string? customerCMND = null,
        int? employeeId = null)
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
