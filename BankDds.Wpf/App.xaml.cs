using System.Windows;

namespace BankDds.Wpf
{
    /// <summary>
    /// Application entry point that initializes the WPF runtime and bootstrap process.
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
