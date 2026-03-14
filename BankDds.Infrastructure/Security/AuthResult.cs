namespace BankDds.Infrastructure.Security
{
    /// <summary>
    /// Represents authentication output used to build runtime user session.
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string UserGroup { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = string.Empty;
        public string? CustomerCMND { get; set; }
        public string? EmployeeId { get; set; }
        public string? DisplayName { get; set; }
    }
}
