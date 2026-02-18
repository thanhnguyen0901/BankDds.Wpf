using System.IO;
using System.Reflection;
using System.Windows;
using Autofac;
using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using BankDds.Core.Interfaces;
using BankDds.Core.Validators;
using BankDds.Infrastructure.Configuration;
using BankDds.Infrastructure.Data;
using BankDds.Infrastructure.Data.InMemory;
using BankDds.Infrastructure.Data.Sql;
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
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();

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

        // Read DataMode from configuration
        var dataMode = configuration["DataMode"] ?? "InMemory";

        // Register repositories based on DataMode
        if (dataMode.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        {
            // InMemory repositories for development and testing
            builder.RegisterType<InMemoryCustomerRepository>()
                   .As<ICustomerRepository>()
                   .SingleInstance();

            builder.RegisterType<InMemoryAccountRepository>()
                   .As<IAccountRepository>()
                   .SingleInstance();

            builder.RegisterType<InMemoryEmployeeRepository>()
                   .As<IEmployeeRepository>()
                   .SingleInstance();

            builder.RegisterType<InMemoryTransactionRepository>()
                   .As<ITransactionRepository>()
                   .SingleInstance();

            builder.RegisterType<InMemoryUserRepository>()
                   .As<IUserRepository>()
                   .SingleInstance();

            builder.RegisterType<InMemoryReportRepository>()
                   .As<IReportRepository>()
                   .SingleInstance();
        }
        else if (dataMode.Equals("Sql", StringComparison.OrdinalIgnoreCase))
        {
            // SQL repositories for production — each operation throws InvalidOperationException
            // with a user-friendly message if the database is unreachable, so the app starts
            // cleanly even when no DB is available yet.
            builder.RegisterType<SqlCustomerRepository>()
                   .As<ICustomerRepository>()
                   .InstancePerDependency();

            builder.RegisterType<SqlAccountRepository>()
                   .As<IAccountRepository>()
                   .InstancePerDependency();

            builder.RegisterType<SqlEmployeeRepository>()
                   .As<IEmployeeRepository>()
                   .InstancePerDependency();

            builder.RegisterType<SqlTransactionRepository>()
                   .As<ITransactionRepository>()
                   .InstancePerDependency();

            builder.RegisterType<SqlUserRepository>()
                   .As<IUserRepository>()
                   .InstancePerDependency();

            builder.RegisterType<SqlReportRepository>()
                   .As<IReportRepository>()
                   .InstancePerDependency();
        }
        else
        {
            throw new InvalidOperationException($"Invalid DataMode '{dataMode}'. Must be 'InMemory' or 'Sql'.");
        }

        // Business services layer - these wrap repositories with additional logic
        builder.RegisterType<CustomerService>()
               .As<ICustomerService>()
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

        // Unified authentication service using IUserRepository
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
