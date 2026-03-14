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
        var normalizedSelectedBranch = NormalizeBranchCode(selectedBranch);
        var normalizedPermittedBranches = (permittedBranches ?? new List<string>())
            .Where(static b => !string.IsNullOrWhiteSpace(b))
            .Select(NormalizeBranchCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Username = username;
        DisplayName = displayName;
        UserGroup = userGroup;
        SelectedBranch = normalizedSelectedBranch;
        PermittedBranches = normalizedPermittedBranches;
        CustomerCMND = customerCMND;
        EmployeeId = employeeId;
        IsAuthenticated = true;
    }

    public void SetSelectedBranch(string branchCode)
    {
        var normalizedBranchCode = NormalizeBranchCode(branchCode);

        if (!PermittedBranches.Contains(normalizedBranchCode, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Chi nhánh '{normalizedBranchCode}' không nằm trong danh sách được phép của phiên làm việc này.");

        if (string.Equals(SelectedBranch, normalizedBranchCode, StringComparison.OrdinalIgnoreCase))
            return;

        SelectedBranch = normalizedBranchCode;
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

    private static string NormalizeBranchCode(string branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode))
            throw new ArgumentException("Mã chi nhánh không được để trống.", nameof(branchCode));

        return branchCode.Trim().ToUpperInvariant();
    }
}

