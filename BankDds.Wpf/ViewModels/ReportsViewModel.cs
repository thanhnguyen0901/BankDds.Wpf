using Caliburn.Micro;
using BankDds.Core.Interfaces;
using BankDds.Core.Models;
using BankDds.Wpf.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace BankDds.Wpf.ViewModels;

public class ReportsViewModel : Screen
{
    private readonly IReportService _reportService;
    private readonly IUserSession _userSession;
    private readonly IAccountService _accountService;
    private readonly IReportExportService _exportService;

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

    // Transaction Summary fields
    private DateTime _transactionSummaryFromDate = DateTime.Now.AddMonths(-1);
    private DateTime _transactionSummaryToDate = DateTime.Now;
    private string _selectedBranchForTransactionSummary = "ALL";
    private TransactionSummary? _transactionSummary;

    public ReportsViewModel(IReportService reportService, IUserSession userSession, IAccountService accountService, IReportExportService exportService)
    {
        _reportService = reportService;
        _userSession = userSession;
        _accountService = accountService;
        _exportService = exportService;
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
            NotifyOfPropertyChange(() => CanExportStatementToPdf);
            NotifyOfPropertyChange(() => CanExportStatementToExcel);
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
            NotifyOfPropertyChange(() => CanExportAccountsToPdf);
            NotifyOfPropertyChange(() => CanExportAccountsToExcel);
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
            NotifyOfPropertyChange(() => CanExportCustomersToPdf);
            NotifyOfPropertyChange(() => CanExportCustomersToExcel);
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

    // Export button states
    public bool CanExportStatementToPdf => HasStatement;
    public bool CanExportStatementToExcel => HasStatement;
    public bool CanExportAccountsToPdf => AccountsOpened.Any();
    public bool CanExportAccountsToExcel => AccountsOpened.Any();
    public bool CanExportCustomersToPdf => CustomersByBranch.Any();
    public bool CanExportCustomersToExcel => CustomersByBranch.Any();

    // Transaction Summary Properties
    public DateTime TransactionSummaryFromDate
    {
        get => _transactionSummaryFromDate;
        set
        {
            _transactionSummaryFromDate = value;
            NotifyOfPropertyChange(() => TransactionSummaryFromDate);
        }
    }

    public DateTime TransactionSummaryToDate
    {
        get => _transactionSummaryToDate;
        set
        {
            _transactionSummaryToDate = value;
            NotifyOfPropertyChange(() => TransactionSummaryToDate);
        }
    }

    public string SelectedBranchForTransactionSummary
    {
        get => _selectedBranchForTransactionSummary;
        set
        {
            _selectedBranchForTransactionSummary = value;
            NotifyOfPropertyChange(() => SelectedBranchForTransactionSummary);
        }
    }

    public TransactionSummary? TransactionSummary
    {
        get => _transactionSummary;
        set
        {
            _transactionSummary = value;
            NotifyOfPropertyChange(() => TransactionSummary);
            NotifyOfPropertyChange(() => HasTransactionSummary);
            NotifyOfPropertyChange(() => CanExportTransactionSummaryToPdf);
            NotifyOfPropertyChange(() => CanExportTransactionSummaryToExcel);
        }
    }

    public bool HasTransactionSummary => TransactionSummary != null;
    public bool CanExportTransactionSummaryToPdf => HasTransactionSummary;
    public bool CanExportTransactionSummaryToExcel => HasTransactionSummary;

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

        // Validate date range before calling the service.
        if (StatementFromDate.Date > StatementToDate.Date)
        {
            ErrorMessage = "From Date must be on or before To Date.";
            AccountStatement = null;
            return;
        }

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

            // DE3-style summary: show opening, line count, closing.
            ErrorMessage = $"Statement generated — {statement.Lines.Count} transaction(s). " +
                           $"Opening: {statement.OpeningBalance:N0} VND  →  " +
                           $"Closing: {statement.ClosingBalance:N0} VND";
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

            // UI branch-picker filter (role-scoping is already enforced by the service layer)
            if (SelectedBranchForAccounts != "ALL")
            {
                accounts = accounts.Where(a => a.MACN == SelectedBranchForAccounts).ToList();
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

    public async Task GenerateTransactionSummaryReport()
    {
        try
        {
            string? branchFilter = SelectedBranchForTransactionSummary == "ALL" ? null : SelectedBranchForTransactionSummary;
            
            // Apply role-based filtering
            if (_userSession.UserGroup == UserGroup.ChiNhanh)
            {
                branchFilter = _userSession.SelectedBranch;
            }

            var summary = await _reportService.GetTransactionSummaryAsync(
                TransactionSummaryFromDate,
                TransactionSummaryToDate,
                branchFilter);
            
            TransactionSummary = summary;
            ErrorMessage = $"Transaction Summary: {summary.TotalTransactionCount} transactions found " +
                          $"({summary.DepositCount} deposits, {summary.WithdrawalCount} withdrawals, {summary.TransferCount} transfers).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error generating transaction summary report: {ex.Message}";
        }
    }

    public async Task ExportStatementToPdf()
    {
        if (AccountStatement == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"AccountStatement_{AccountStatement.SOTK}_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Export Account Statement to PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportStatementToPdfAsync(AccountStatement, dialog.FileName);
                ErrorMessage = $"? Statement exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportStatementToExcel()
    {
        if (AccountStatement == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"AccountStatement_{AccountStatement.SOTK}_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Export Account Statement to Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportStatementToExcelAsync(AccountStatement, dialog.FileName);
                ErrorMessage = $"? Statement exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportAccountsToPdf()
    {
        if (!AccountsOpened.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"AccountsOpened_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Export Accounts Report to PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportAccountsToPdfAsync(
                    AccountsOpened.ToList(), 
                    AccountsOpenedFromDate, 
                    AccountsOpenedToDate, 
                    dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportAccountsToExcel()
    {
        if (!AccountsOpened.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"AccountsOpened_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Export Accounts Report to Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportAccountsToExcelAsync(
                    AccountsOpened.ToList(), 
                    AccountsOpenedFromDate, 
                    AccountsOpenedToDate, 
                    dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportCustomersToPdf()
    {
        if (!CustomersByBranch.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"CustomersByBranch_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Export Customers Report to PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportCustomersToPdfAsync(
                    CustomersByBranch.ToList(), 
                    SelectedBranchForCustomers == "ALL" ? null : SelectedBranchForCustomers, 
                    dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportCustomersToExcel()
    {
        if (!CustomersByBranch.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"CustomersByBranch_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Export Customers Report to Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportCustomersToExcelAsync(
                    CustomersByBranch.ToList(), 
                    SelectedBranchForCustomers == "ALL" ? null : SelectedBranchForCustomers, 
                    dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportTransactionSummaryToPdf()
    {
        if (TransactionSummary == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = $"TransactionSummary_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Export Transaction Summary to PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportTransactionSummaryToPdfAsync(TransactionSummary, dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportTransactionSummaryToExcel()
    {
        if (TransactionSummary == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Excel Files (*.xlsx)|*.xlsx",
            FileName = $"TransactionSummary_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Export Transaction Summary to Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportTransactionSummaryToExcelAsync(TransactionSummary, dialog.FileName);
                ErrorMessage = $"? Report exported successfully to {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error exporting to Excel: {ex.Message}";
            }
        }
    }
}
