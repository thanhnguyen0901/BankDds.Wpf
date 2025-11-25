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
    
    void SetSession(
        string username,
        string displayName,
        UserGroup userGroup,
        string selectedBranch,
        List<string> permittedBranches,
        string? customerCMND = null);
    
    void ClearSession();
    
    bool IsAuthenticated { get; }
}
