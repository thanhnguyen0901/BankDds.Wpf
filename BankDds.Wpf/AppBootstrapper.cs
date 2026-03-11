using System.IO;
using System.Reflection;
using System.Windows;
using Autofac;
using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BankDds.Core.Interfaces;
using BankDds.Core.Validators;
using BankDds.Infrastructure.Configuration;
using BankDds.Infrastructure.Data;
using BankDds.Infrastructure.Security;
using BankDds.Wpf.ViewModels;
using BankDds.Wpf.Services;

namespace BankDds.Wpf;

public class AppBootstrapper : BootstrapperBase
{
    private IContainer? _container;

    public AppBootstrapper()
    {
        Initialize();
    }

    protected override void Configure()
    {
        // Configure Caliburn.Micro ViewLocator to handle custom view locations
        ConfigureViewLocator();

        var builder = new ContainerBuilder();

        // Configuration
        // Priority (highest -> lowest):
        //   1. Environment variables (BANKDDS_CONNSTR_* or standard ConnectionStrings__* style)
        //   2. appsettings.Development.json  (git-ignored - local dev secrets)
        //   3. appsettings.json              (committed - placeholders only, no real passwords)
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

        // Logging - routes to Visual Studio Debug Output (no-op in Release unless AddConsole() is wired)
        var loggerFactory = LoggerFactory.Create(lb =>
        {
            lb.SetMinimumLevel(LogLevel.Debug);
            lb.AddDebug();
        });
        builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
        // Open-generic registration: any ILogger<T> resolved from the container
        // is created via the factory so all categories share the same provider chain.
        builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();

        // Caliburn services
        builder.RegisterType<EventAggregator>()
               .As<IEventAggregator>()
               .SingleInstance();

        builder.RegisterType<WindowManager>()
               .As<IWindowManager>()
               .SingleInstance();

        // Dialog service
        builder.RegisterType<DialogService>()
               .As<IDialogService>()
               .SingleInstance();

        // Report Export service
        builder.RegisterType<ReportExportService>()
               .As<IReportExportService>()
               .SingleInstance();

        // Configuration services
        builder.RegisterType<ConnectionStringProvider>()
               .As<IConnectionStringProvider>()
               .SingleInstance();

        // User session
        builder.RegisterType<UserSession>()
               .As<IUserSession>()
               .SingleInstance();

        // Authorization service
        builder.RegisterType<AuthorizationService>()
               .As<IAuthorizationService>()
               .SingleInstance();

        // Validators - Register as singletons (they're stateless)
        builder.RegisterType<CustomerValidator>().AsSelf().SingleInstance();
        builder.RegisterType<AccountValidator>().AsSelf().SingleInstance();
        builder.RegisterType<EmployeeValidator>().AsSelf().SingleInstance();
        builder.RegisterType<TransactionValidator>().AsSelf().SingleInstance();
        builder.RegisterType<UserValidator>().AsSelf().SingleInstance();
        builder.RegisterType<BranchValidator>().AsSelf().SingleInstance();

        // Data access repositories (SQL Server distributed setup only).
        builder.RegisterType<CustomerRepository>()
               .As<ICustomerRepository>()
               .InstancePerDependency();

        builder.RegisterType<AccountRepository>()
               .As<IAccountRepository>()
               .InstancePerDependency();

        builder.RegisterType<EmployeeRepository>()
               .As<IEmployeeRepository>()
               .InstancePerDependency();

        builder.RegisterType<TransactionRepository>()
               .As<ITransactionRepository>()
               .InstancePerDependency();

        builder.RegisterType<UserRepository>()
               .As<IUserRepository>()
               .InstancePerDependency();

        builder.RegisterType<ReportRepository>()
               .As<IReportRepository>()
               .InstancePerDependency();

        // BranchRepository only holds IConnectionStringProvider (singleton),
        // so SingleInstance is safe and avoids captive-dependency issues in validators.
        builder.RegisterType<BranchRepository>()
               .As<IBranchRepository>()
               .SingleInstance();

        builder.RegisterType<CustomerLookupRepository>()
               .As<ICustomerLookupRepository>()
               .InstancePerDependency();

        // Business services layer - these wrap repositories with additional logic
        builder.RegisterType<CustomerService>()
               .As<ICustomerService>()
               .SingleInstance();

        builder.RegisterType<BranchService>()
               .As<IBranchService>()
               .SingleInstance();

        builder.RegisterType<AccountService>()
               .As<IAccountService>()
               .SingleInstance();

        builder.RegisterType<EmployeeService>()
               .As<IEmployeeService>()
               .SingleInstance();

        builder.RegisterType<TransactionService>()
               .As<ITransactionService>()
               .SingleInstance();

        builder.RegisterType<ReportService>()
               .As<IReportService>()
               .SingleInstance();

        builder.RegisterType<UserService>()
               .As<IUserService>()
               .SingleInstance();

        builder.RegisterType<CustomerLookupService>()
               .As<ICustomerLookupService>()
               .SingleInstance();

        // Banking authentication: SQL login -> sp_DangNhap on Publisher
        builder.RegisterType<AuthService>()
               .As<IAuthService>()
               .SingleInstance();

        // Shell - SingleInstance to maintain state
        builder.RegisterType<MainShellViewModel>()
               .AsSelf()
               .SingleInstance();

        // ViewModels - register all types ending with ViewModel
        builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
               .Where(t => t.Name.EndsWith("ViewModel") && t != typeof(MainShellViewModel))
               .AsSelf()
               .InstancePerDependency();

        _container = builder.Build();
    }

    private void ConfigureViewLocator()
    {
        // Store the original transform function
        var originalTransform = ViewLocator.LocateTypeForModelType;

        // Override the ViewLocator to handle custom locations
        ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
        {
            // Special handling for MainShellViewModel
            if (modelType == typeof(MainShellViewModel))
            {
                // Search for MainShellView in the current assembly
                var viewType = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => t.Name == "MainShellView" && t.Namespace == "BankDds.Wpf.Shell");

                if (viewType != null)
                    return viewType;
            }

            // Fall back to default behavior
            return originalTransform(modelType, displayLocation, context);
        };
    }

    protected override object GetInstance(Type service, string key)
    {
        if (_container == null)
            throw new InvalidOperationException("Container is not initialized");

        return _container.Resolve(service);
    }

    protected override IEnumerable<object> GetAllInstances(Type service)
    {
        if (_container == null)
            throw new InvalidOperationException("Container is not initialized");

        var enumerableType = typeof(IEnumerable<>).MakeGenericType(service);
        return (IEnumerable<object>)_container.Resolve(enumerableType);
    }

    protected override void BuildUp(object instance)
    {
        if (_container == null)
            throw new InvalidOperationException("Container is not initialized");

        _container.InjectProperties(instance);
    }

    protected override async void OnStartup(object sender, StartupEventArgs e)
    {
        // Start with MainShell which will handle navigation
        await DisplayRootViewForAsync<MainShellViewModel>();
    }

    protected override void OnExit(object sender, EventArgs e)
    {
        _container?.Dispose();
        base.OnExit(sender, e);
    }
}
