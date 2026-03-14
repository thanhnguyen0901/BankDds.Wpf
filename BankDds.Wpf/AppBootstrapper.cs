using Autofac;
using BankDds.Core.Interfaces;
using BankDds.Core.Validators;
using BankDds.Infrastructure.Configuration;
using BankDds.Infrastructure.Data;
using BankDds.Infrastructure.Security;
using BankDds.Wpf.Services;
using BankDds.Wpf.ViewModels;
using Caliburn.Micro;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Reflection;
using System.Windows;

namespace BankDds.Wpf
{
    public class AppBootstrapper : BootstrapperBase
    {
        private IContainer? _container;

        public AppBootstrapper()
        {
            Initialize();
        }

        protected override void Configure()
        {
            ConfigureViewLocator();
            var builder = new ContainerBuilder();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            builder.RegisterInstance(configuration).As<IConfiguration>().SingleInstance();
            var loggerFactory = LoggerFactory.Create(lb =>
            {
                lb.SetMinimumLevel(LogLevel.Debug);
                lb.AddDebug();
            });
            builder.RegisterInstance(loggerFactory).As<ILoggerFactory>().SingleInstance();
            builder.RegisterGeneric(typeof(Logger<>)).As(typeof(ILogger<>)).SingleInstance();
            builder.RegisterType<EventAggregator>()
                   .As<IEventAggregator>()
                   .SingleInstance();
            builder.RegisterType<WindowManager>()
                   .As<IWindowManager>()
                   .SingleInstance();
            builder.RegisterType<DialogService>()
                   .As<IDialogService>()
                   .SingleInstance();
            builder.RegisterType<ReportExportService>()
                   .As<IReportExportService>()
                   .SingleInstance();
            builder.RegisterType<ConnectionStringProvider>()
                   .As<IConnectionStringProvider>()
                   .SingleInstance();
            builder.RegisterType<UserSession>()
                   .As<IUserSession>()
                   .SingleInstance();
            builder.RegisterType<AuthorizationService>()
                   .As<IAuthorizationService>()
                   .SingleInstance();
            builder.RegisterType<CustomerValidator>().AsSelf().SingleInstance();
            builder.RegisterType<AccountValidator>().AsSelf().SingleInstance();
            builder.RegisterType<EmployeeValidator>().AsSelf().SingleInstance();
            builder.RegisterType<TransactionValidator>().AsSelf().SingleInstance();
            builder.RegisterType<UserValidator>().AsSelf().SingleInstance();
            builder.RegisterType<BranchValidator>().AsSelf().SingleInstance();
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
            builder.RegisterType<BranchRepository>()
                   .As<IBranchRepository>()
                   .SingleInstance();
            builder.RegisterType<CustomerLookupRepository>()
                   .As<ICustomerLookupRepository>()
                   .InstancePerDependency();
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
            builder.RegisterType<AuthService>()
                   .As<IAuthService>()
                   .SingleInstance();
            builder.RegisterType<MainShellViewModel>()
                   .AsSelf()
                   .SingleInstance();
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                   .Where(t => t.Name.EndsWith("ViewModel") && t != typeof(MainShellViewModel))
                   .AsSelf()
                   .InstancePerDependency();
            _container = builder.Build();
        }

        private void ConfigureViewLocator()
        {
            var originalTransform = ViewLocator.LocateTypeForModelType;
            ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
            {
                if (modelType == typeof(MainShellViewModel))
                {
                    var viewType = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .FirstOrDefault(t => t.Name == "MainShellView" && t.Namespace == "BankDds.Wpf.Shell");
                    if (viewType != null)
                        return viewType;
                }
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
            await DisplayRootViewForAsync<MainShellViewModel>();
        }

        protected override void OnExit(object sender, EventArgs e)
        {
            _container?.Dispose();
            base.OnExit(sender, e);
        }
    }
}