namespace BankDds.Core.Interfaces
{
    public interface IConnectionStringProvider
    {
        string GetConnectionStringForBranch(string branch);
        string GetPublisherConnection();
        [System.Obsolete("Use GetPublisherConnection() instead.")]
        string GetBankConnection();
        string GetPublisherConnectionForLogin(string sqlLogin, string sqlPassword);
        void SetSqlLoginCredentials(string sqlLogin, string sqlPassword);
        void ClearSqlLoginCredentials();
        string? GetLookupConnection();
        IReadOnlyList<string> GetConfiguredBranchCodes();
        string DefaultBranch { get; }
    }
}