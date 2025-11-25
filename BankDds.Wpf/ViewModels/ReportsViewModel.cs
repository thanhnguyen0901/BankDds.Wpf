using Caliburn.Micro;
using BankDds.Core.Interfaces;

namespace BankDds.Wpf.ViewModels;

public class ReportsViewModel : Screen
{
    private readonly IReportService _reportService;

    public ReportsViewModel(IReportService reportService)
    {
        _reportService = reportService;
        DisplayName = "Reports";
    }
}
