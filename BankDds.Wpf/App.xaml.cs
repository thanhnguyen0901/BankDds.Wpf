using System.Windows;

namespace BankDds.Wpf
{
    /// <summary>
    /// Handles App responsibilities in the application.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            new AppBootstrapper();
        }
    }
}
