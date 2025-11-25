using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using System.Collections.ObjectModel;

namespace BankDds.Wpf.ViewModels;

public class ReportsViewModel : Screen
{
    private readonly IReportService _reportService;
    private readonly IUserSession _userSession;
    private readonly IAccountService _accountService;

    // Account Statement fields
    private string _statementAccountNumber = string.Empty;
    private DateTime _statementFromDate = DateTime.Now.AddMonths(-1);
    private DateTime _statementToDate = DateTime.Now;
    private AccountStatement? _accountStatement;
    private string _errorMessage = string.Empty;

    // Accounts Opened fields
    private DateTime _accountsOpenedFromDate = DateTime.Now.AddMonths(-1);
    private DateTime _accountsOpenedToDate = DateTime.Now;
    private string _selectedBranchForAccounts = "ALL";
    private ObservableCollection<Account> _accountsOpened = new();

    // Customers Per Branch fields
    private string _selectedBranchForCustomers = "ALL";
    private ObservableCollection<Customer> _customersByBranch = new();

    public ReportsViewModel(IReportService reportService, IUserSession userSession, IAccountService accountService)
    {
        _reportService = reportService;
        _userSession = userSession;
        _accountService = accountService;
        DisplayName = "Reports";
    }

    // Account Statement Properties
    public string StatementAccountNumber
    {
        get => _statementAccountNumber;
        set
        {
            _statementAccountNumber = value;
            NotifyOfPropertyChange(() => StatementAccountNumber);
            NotifyOfPropertyChange(() => CanGenerateStatement);
        }
    }

    public DateTime StatementFromDate
    {
        get => _statementFromDate;
        set
        {
            _statementFromDate = value;
            NotifyOfPropertyChange(() => StatementFromDate);
        }
    }

    public DateTime StatementToDate
    {
        get => _statementToDate;
        set
        {
            _statementToDate = value;
            NotifyOfPropertyChange(() => StatementToDate);
        }
    }

    public AccountStatement? AccountStatement
    {
        get => _accountStatement;
        set
        {
            _accountStatement = value;
            NotifyOfPropertyChange(() => AccountStatement);
            NotifyOfPropertyChange(() => HasStatement);
        }
    }

    public bool HasStatement => AccountStatement != null;

    public bool CanGenerateStatement => !string.IsNullOrWhiteSpace(StatementAccountNumber);

    // Accounts Opened Properties
    public DateTime AccountsOpenedFromDate
    {
        get => _accountsOpenedFromDate;
        set
        {
            _accountsOpenedFromDate = value;
            NotifyOfPropertyChange(() => AccountsOpenedFromDate);
        }
    }

    public DateTime AccountsOpenedToDate
    {
        get => _accountsOpenedToDate;
        set
        {
            _accountsOpenedToDate = value;
            NotifyOfPropertyChange(() => AccountsOpenedToDate);
        }
    }

    public string SelectedBranchForAccounts
    {
        get => _selectedBranchForAccounts;
        set
        {
            _selectedBranchForAccounts = value;
            NotifyOfPropertyChange(() => SelectedBranchForAccounts);
        }
    }

    public ObservableCollection<Account> AccountsOpened
    {
        get => _accountsOpened;
        set
        {
            _accountsOpened = value;
            NotifyOfPropertyChange(() => AccountsOpened);
        }
    }

    // Customers Per Branch Properties
    public string SelectedBranchForCustomers
    {
        get => _selectedBranchForCustomers;
        set
        {
            _selectedBranchForCustomers = value;
            NotifyOfPropertyChange(() => SelectedBranchForCustomers);
        }
    }

    public ObservableCollection<Customer> CustomersByBranch
    {
        get => _customersByBranch;
        set
        {
            _customersByBranch = value;
            NotifyOfPropertyChange(() => CustomersByBranch);
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            NotifyOfPropertyChange(() => ErrorMessage);
            NotifyOfPropertyChange(() => HasError);
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public ObservableCollection<string> AvailableBranches { get; } = new() { "ALL", "BENTHANH", "TANDINH" };

    protected override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await base.OnActivateAsync(cancellationToken);
        
        // For customer mode, pre-populate with their accounts
        if (_userSession.UserGroup == UserGroup.KhachHang)
        {
            await LoadCustomerAccountsAsync();
        }
    }

    private async Task LoadCustomerAccountsAsync()
    {
        try
        {
            var accounts = await _accountService.GetAccountsByCustomerAsync(_userSession.CustomerCMND ?? "");
            if (accounts.Any())
            {
                StatementAccountNumber = accounts.First().SOTK;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error loading accounts: {ex.Message}";
        }
    }

    public async Task GenerateStatement()
    {
        if (!CanGenerateStatement) return;

        try
        {
            // Check if customer is allowed to view this account
            if (_userSession.UserGroup == UserGroup.KhachHang)
            {
                var customerAccounts = await _accountService.GetAccountsByCustomerAsync(_userSession.CustomerCMND ?? "");
                if (!customerAccounts.Any(a => a.SOTK == StatementAccountNumber))
                {
                    ErrorMessage = "You are not authorized to view this account statement.";
                    return;
                }
            }

            var statement = await _reportService.GetAccountStatementAsync(
                StatementAccountNumber, 
                StatementFromDate, 
                StatementToDate);
            
            AccountStatement = statement;
            ErrorMessage = "Account statement generated successfully.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating statement: {ex.Message}";
            AccountStatement = null;
        }
    }

    public async Task GenerateAccountsOpenedReport()
    {
        try
        {
            var accounts = await _reportService.GetAccountsOpenedInPeriodAsync(
                AccountsOpenedFromDate, 
                AccountsOpenedToDate);

            // Filter by branch if selected
            if (SelectedBranchForAccounts != "ALL")
            {
                accounts = accounts.Where(a => a.MACN == SelectedBranchForAccounts).ToList();
            }

            // Apply role-based filtering
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
            {
                accounts = accounts.Where(a => a.MACN == _userSession.SelectedBranch).ToList();
            }

            AccountsOpened = new ObservableCollection<Account>(accounts);
            ErrorMessage = $"Found {accounts.Count} accounts opened in the period.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating accounts opened report: {ex.Message}";
        }
    }

    public async Task GenerateCustomersPerBranchReport()
    {
        try
        {
            string? branchFilter = SelectedBranchForCustomers == "ALL" ? null : SelectedBranchForCustomers;
            
            // Apply role-based filtering
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
            {
                branchFilter = _userSession.SelectedBranch;
            }

            var customers = await _reportService.GetCustomersByBranchReportAsync(branchFilter);
            CustomersByBranch = new ObservableCollection<Customer>(customers);
            ErrorMessage = $"Found {customers.Count} customers.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating customers per branch report: {ex.Message}";
        }
    }
}
