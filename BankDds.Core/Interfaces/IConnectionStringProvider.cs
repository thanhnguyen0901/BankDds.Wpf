namespace BankDds.Core.Interfaces;

public interface IConnectionStringProvider
{
    string GetConnectionStringForBranch(string branch);
    string GetBankConnection();
    string DefaultBranch { get; }
}
