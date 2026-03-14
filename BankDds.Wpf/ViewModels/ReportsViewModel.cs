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
        DisplayName = "Báo cáo";
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

    public ObservableCollection<string> AvailableBranches { get; } = new();
    public bool IsCustomerMode => _userSession.UserGroup == UserGroup.KhachHang;
    public bool CanViewManagementReports => _userSession.UserGroup != UserGroup.KhachHang;

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
        InitializeBranchFilters();
        
        // For customer mode, pre-populate with their accounts
        if (_userSession.UserGroup == UserGroup.KhachHang)
        {
            await LoadCustomerAccountsAsync();
        }
    }

    private void InitializeBranchFilters()
    {
        AvailableBranches.Clear();

        if (_userSession.UserGroup == UserGroup.NganHang)
        {
            AvailableBranches.Add("ALL");
            foreach (var branch in _userSession.PermittedBranches
                         .Distinct(StringComparer.OrdinalIgnoreCase)
                         .OrderBy(static b => b, StringComparer.OrdinalIgnoreCase))
            {
                AvailableBranches.Add(branch);
            }
        }
        else if (!string.IsNullOrWhiteSpace(_userSession.SelectedBranch))
        {
            AvailableBranches.Add(_userSession.SelectedBranch);
        }

        if (AvailableBranches.Count == 0)
        {
            AvailableBranches.Add("ALL");
        }

        SelectedBranchForAccounts = AvailableBranches[0];
        SelectedBranchForCustomers = AvailableBranches[0];
        SelectedBranchForTransactionSummary = AvailableBranches[0];

        NotifyOfPropertyChange(() => IsCustomerMode);
        NotifyOfPropertyChange(() => CanViewManagementReports);
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
            ErrorMessage = $"Lỗi tải danh sách tài khoản: {ex.Message}";
        }
    }

    public async Task GenerateStatement()
    {
        if (!CanGenerateStatement) return;

        // Validate date range before calling the service.
        if (StatementFromDate.Date > StatementToDate.Date)
        {
            ErrorMessage = "Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc.";
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
                    ErrorMessage = "Bạn không có quyền xem sao kê của tài khoản này.";
                    return;
                }
            }

            var statement = await _reportService.GetAccountStatementAsync(
                StatementAccountNumber,
                StatementFromDate,
                StatementToDate);

            AccountStatement = statement;

            // DE3-style summary: show opening, line count, closing.
            ErrorMessage = $"Đã tạo sao kê - {statement.Lines.Count} giao dịch. " +
                           $"Số dư đầu: {statement.OpeningBalance:N0} VND -> " +
                           $"Số dư cuối: {statement.ClosingBalance:N0} VND";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo sao kê: {ex.Message}";
            AccountStatement = null;
        }
    }

    public async Task GenerateAccountsOpenedReport()
    {
        if (!CanViewManagementReports)
        {
            ErrorMessage = "Khách hàng không có quyền xem báo cáo này.";
            return;
        }

        try
        {
            var accounts = await _reportService.GetAccountsOpenedInPeriodAsync(
                AccountsOpenedFromDate,
                AccountsOpenedToDate,
                SelectedBranchForAccounts);

            AccountsOpened = new ObservableCollection<Account>(accounts);
            ErrorMessage = $"Tìm thấy {accounts.Count} tài khoản mở trong khoảng thời gian đã chọn.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo báo cáo tài khoản mở: {ex.Message}";
        }
    }

    public async Task GenerateCustomersPerBranchReport()
    {
        if (!CanViewManagementReports)
        {
            ErrorMessage = "Khách hàng không có quyền xem báo cáo này.";
            return;
        }

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
            ErrorMessage = $"Tìm thấy {customers.Count} khách hàng.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo báo cáo khách hàng theo chi nhánh: {ex.Message}";
        }
    }

    public async Task GenerateTransactionSummaryReport()
    {
        if (!CanViewManagementReports)
        {
            ErrorMessage = "Khách hàng không có quyền xem báo cáo này.";
            return;
        }

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
            ErrorMessage = $"Tổng hợp giao dịch: tìm thấy {summary.TotalTransactionCount} giao dịch " +
                          $"({summary.DepositCount} gửi tiền, {summary.WithdrawalCount} rút tiền, {summary.TransferCount} chuyển tiền).";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo báo cáo tổng hợp giao dịch: {ex.Message}";
        }
    }

    public async Task ExportStatementToPdf()
    {
        if (AccountStatement == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp PDF (*.pdf)|*.pdf",
            FileName = $"AccountStatement_{AccountStatement.SOTK}_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Xuất sao kê tài khoản ra PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportStatementToPdfAsync(AccountStatement, dialog.FileName);
                ErrorMessage = $"Xuất sao kê thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportStatementToExcel()
    {
        if (AccountStatement == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            FileName = $"AccountStatement_{AccountStatement.SOTK}_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Xuất sao kê tài khoản ra Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportStatementToExcelAsync(AccountStatement, dialog.FileName);
                ErrorMessage = $"Xuất sao kê thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportAccountsToPdf()
    {
        if (!AccountsOpened.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp PDF (*.pdf)|*.pdf",
            FileName = $"AccountsOpened_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Xuất báo cáo tài khoản ra PDF"
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
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportAccountsToExcel()
    {
        if (!AccountsOpened.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            FileName = $"AccountsOpened_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Xuất báo cáo tài khoản ra Excel"
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
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportCustomersToPdf()
    {
        if (!CustomersByBranch.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp PDF (*.pdf)|*.pdf",
            FileName = $"CustomersByBranch_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Xuất báo cáo khách hàng ra PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportCustomersToPdfAsync(
                    CustomersByBranch.ToList(), 
                    SelectedBranchForCustomers == "ALL" ? null : SelectedBranchForCustomers, 
                    dialog.FileName);
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportCustomersToExcel()
    {
        if (!CustomersByBranch.Any()) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            FileName = $"CustomersByBranch_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Xuất báo cáo khách hàng ra Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportCustomersToExcelAsync(
                    CustomersByBranch.ToList(), 
                    SelectedBranchForCustomers == "ALL" ? null : SelectedBranchForCustomers, 
                    dialog.FileName);
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất Excel: {ex.Message}";
            }
        }
    }

    public async Task ExportTransactionSummaryToPdf()
    {
        if (TransactionSummary == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp PDF (*.pdf)|*.pdf",
            FileName = $"TransactionSummary_{DateTime.Now:yyyyMMdd}.pdf",
            Title = "Xuất tổng hợp giao dịch ra PDF"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportTransactionSummaryToPdfAsync(TransactionSummary, dialog.FileName);
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất PDF: {ex.Message}";
            }
        }
    }

    public async Task ExportTransactionSummaryToExcel()
    {
        if (TransactionSummary == null) return;

        var dialog = new SaveFileDialog
        {
            Filter = "Tệp Excel (*.xlsx)|*.xlsx",
            FileName = $"TransactionSummary_{DateTime.Now:yyyyMMdd}.xlsx",
            Title = "Xuất tổng hợp giao dịch ra Excel"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _exportService.ExportTransactionSummaryToExcelAsync(TransactionSummary, dialog.FileName);
                ErrorMessage = $"Xuất báo cáo thành công: {Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xuất Excel: {ex.Message}";
            }
        }
    }
}



