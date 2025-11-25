using System.Windows;

namespace BankDds.Wpf;

public partial class App : Application
{
    public App()
    {
        new AppBootstrapper();
    }
}
