# BankDds - Distributed Banking System

A .NET 8 WPF application built with **clean 3-layer architecture** using MVVM pattern, Caliburn.Micro, and Autofac for dependency injection. This is a comprehensive banking system with role-based access control, designed for a distributed banking database course project.

## Architecture Overview

The solution follows a **clean 3-layer architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                      BankDds.Wpf                            │
│                   (Presentation Layer)                      │
│  Views, ViewModels, Converters, UI-specific logic          │
│                                                             │
│  Dependencies: → BankDds.Core + BankDds.Infrastructure     │
└─────────────────────────────────────────────────────────────┘
                           ↓ ↓
        ┌──────────────────┘ └──────────────────┐
        ↓                                        ↓
┌──────────────────────┐          ┌──────────────────────────┐
│   BankDds.Core       │          │ BankDds.Infrastructure   │
│   (Domain Layer)     │  ←───────│   (Data Access Layer)    │
│                      │          │                          │
│ • Domain Models      │          │ • Service Implementations│
│ • Service Interfaces │          │ • In-Memory Data         │
│ • Business Rules     │          │ • Authentication         │
│                      │          │ • Configuration          │
│ Dependencies: None   │          │ Dependencies: → Core     │
└──────────────────────┘          └──────────────────────────┘
```

### Project Responsibilities

#### 1. **BankDds.Core** (Domain/Application Core)
- **Purpose**: Contains the **domain models** and **business interfaces**
- **Contents**:
  - `Models/`: Customer, Account, Employee, Transaction, User, AccountStatement, UserGroup
  - `Interfaces/`: ICustomerService, IAccountService, IEmployeeService, ITransactionService, IReportService, IUserService, IUserSession, IConnectionStringProvider
- **Dependencies**: **NONE** - This is the most stable layer
- **Namespace**: `BankDds.Core.Models`, `BankDds.Core.Interfaces`

#### 2. **BankDds.Infrastructure** (Data Access & External Concerns)
- **Purpose**: Implements the interfaces defined in Core; handles data access, authentication, configuration
- **Contents**:
  - `Data/`: In-memory service implementations (CustomerService, AccountService, etc., UserSession)
  - `Security/`: AuthResult, IAuthService, SqlAuthService (authentication logic)
  - `Configuration/`: ConnectionStringProvider (reads from appsettings.json)
- **Dependencies**: → `BankDds.Core` (implements Core interfaces)
- **Namespace**: `BankDds.Infrastructure.Data`, `BankDds.Infrastructure.Security`, `BankDds.Infrastructure.Configuration`

#### 3. **BankDds.Wpf** (Presentation/UI Layer)
- **Purpose**: WPF application with Views, ViewModels, and UI-specific logic
- **Contents**:
  - `ViewModels/`: MainShellViewModel, LoginViewModel, HomeViewModel, CustomersViewModel, etc.
  - `Views/`: XAML files for all screens
  - `Converters/`: BoolToVisibilityConverter (UI helpers)
  - `Shell/`: MainShellView.xaml (main window container)
  - `AppBootstrapper.cs`: Caliburn.Micro + Autofac DI configuration
  - `appsettings.json`: Configuration file
- **Dependencies**: → `BankDds.Core` + `BankDds.Infrastructure`
- **Namespace**: `BankDds.Wpf.ViewModels`, `BankDds.Wpf.Views`, `BankDds.Wpf.Converters`

### Dependency Flow

```
BankDds.Wpf → BankDds.Core (uses interfaces and models)
            → BankDds.Infrastructure (uses concrete implementations)

BankDds.Infrastructure → BankDds.Core (implements interfaces)

BankDds.Core → (NO DEPENDENCIES - pure domain logic)
```

### Benefits of This Architecture

1. **Separation of Concerns**: Each layer has a clear, single responsibility
2. **Testability**: Core business logic is isolated and can be unit tested without UI or database
3. **Maintainability**: Changes in one layer don't ripple through others (e.g., swap in-memory data with SQL Server without touching UI)
4. **Flexibility**: Easy to replace implementations (e.g., in-memory → Dapper → EF Core) without changing Core or UI
5. **Clean Dependency Direction**: Dependencies point inward toward Core, following the Dependency Inversion Principle

## Project Structure

```
BankDds.Wpf/
├── BankDds.Wpf.sln                    # Solution file
├── BankDds.Core/                      # ⭐ NEW: Domain/Application Core
│   ├── BankDds.Core.csproj
│   ├── Models/                        # Domain entities
│   │   ├── UserGroup.cs               # Enum: NganHang, ChiNhanh, KhachHang
│   │   ├── Customer.cs
│   │   ├── Account.cs
│   │   ├── Employee.cs
│   │   ├── Transaction.cs
│   │   ├── User.cs
│   │   └── AccountStatement.cs
│   └── Interfaces/                    # Service contracts
│       ├── IUserSession.cs
│       ├── ICustomerService.cs
│       ├── IAccountService.cs
│       ├── IEmployeeService.cs
│       ├── ITransactionService.cs
│       ├── IReportService.cs
│       ├── IUserService.cs
│       └── IConnectionStringProvider.cs
├── BankDds.Infrastructure/            # ⭐ REFACTORED: Data Access & Infrastructure
│   ├── BankDds.Infrastructure.csproj
│   ├── Data/                          # In-memory service implementations
│   │   ├── UserSession.cs             # Singleton session state
│   │   ├── CustomerService.cs
│   │   ├── AccountService.cs
│   │   ├── EmployeeService.cs
│   │   ├── TransactionService.cs
│   │   ├── ReportService.cs
│   │   └── UserService.cs
│   ├── Security/                      # Authentication
│   │   ├── AuthResult.cs
│   │   ├── IAuthService.cs
│   │   └── SqlAuthService.cs          # Hard-coded test users
│   └── Configuration/                 # Configuration services
│       └── ConnectionStringProvider.cs
└── BankDds.Wpf/                       # ⭐ CLEANED: Pure UI Layer
    ├── BankDds.Wpf.csproj
    ├── App.xaml
    ├── App.xaml.cs
    ├── AppBootstrapper.cs             # Caliburn.Micro + Autofac DI setup
    ├── appsettings.json               # Connection strings and settings
    ├── Converters/
    │   └── BoolToVisibilityConverter.cs
    ├── ViewModels/                    # All ViewModels (pure UI logic)
    │   ├── MainShellViewModel.cs      # Root conductor
    │   ├── LoginViewModel.cs
    │   ├── HomeViewModel.cs
    │   ├── CustomersViewModel.cs
    │   ├── AccountsViewModel.cs
    │   ├── EmployeesViewModel.cs
    │   ├── TransactionsViewModel.cs
    │   ├── ReportsViewModel.cs
    │   └── AdminViewModel.cs
    ├── Views/                         # XAML UI definitions
    │   ├── LoginView.xaml
    │   ├── HomeView.xaml
    │   ├── CustomersView.xaml
    │   ├── AccountsView.xaml
    │   ├── EmployeesView.xaml
    │   ├── TransactionsView.xaml
    │   ├── ReportsView.xaml
    │   └── AdminView.xaml
    └── Shell/
        └── MainShellView.xaml         # Main window container
```

## Technologies

- **.NET 8** - Target framework
- **WPF** - Windows Presentation Foundation
- **Caliburn.Micro 4.0.212** - MVVM framework with conventions
- **Autofac 8.1.0** - Dependency injection container
- **Microsoft.Extensions.Configuration** - Configuration management
- **Microsoft.Data.SqlClient** - SQL Server data provider (ready for integration)
- **Dapper** - Micro ORM (ready for integration)

## Key Design Patterns & Practices

### 1. Clean Architecture
- **Domain-Centric Design**: Core layer contains only domain logic and interfaces
- **Dependency Inversion**: High-level modules (UI) don't depend on low-level modules (Data Access); both depend on abstractions (Core interfaces)
- **Testability**: Core business logic can be tested in isolation

### 2. MVVM Pattern with Caliburn.Micro
- **ViewModels**: Pure C# classes with UI logic (no XAML dependencies)
- **Views**: XAML files for UI presentation
- **Conventions**: Automatic binding by name (`x:Name="Login"` → `Login()` method)
- **Conductor Pattern**: `MainShellViewModel` and `HomeViewModel` manage screen navigation

### 3. Dependency Injection with Autofac
- **Interface-based Programming**: All services accessed through interfaces from Core
- **Lifetime Management**:
  - `SingleInstance`: In-memory data services, UserSession, IAuthService (shared state)
  - `InstancePerDependency`: ViewModels (new instance per navigation)
- **Composition Root**: `AppBootstrapper.cs` registers all dependencies

### 4. Repository Pattern (Ready for Database)
- **In-Memory Implementation**: Current implementation uses `List<T>` for rapid prototyping
- **Interface Abstraction**: All data access through `ICustomerService`, `IAccountService`, etc.
- **Easy Migration**: Swap in-memory implementations with Dapper/EF Core without changing UI or Core

## How to Replace In-Memory Data with Database

The architecture makes database integration straightforward:

**Step 1**: Create new implementations in `BankDds.Infrastructure/Data`:
```csharp
// Example: Replace CustomerService with Dapper implementation
public class DapperCustomerService : ICustomerService
{
    private readonly IConnectionStringProvider _connectionProvider;
    private readonly IUserSession _userSession;
    
    public DapperCustomerService(
        IConnectionStringProvider connectionProvider,
        IUserSession userSession)
    {
        _connectionProvider = connectionProvider;
        _userSession = userSession;
    }
    
    public async Task<List<Customer>> GetCustomersByBranchAsync(string branchCode)
    {
        using var connection = new SqlConnection(
            _connectionProvider.GetConnectionStringForBranch(branchCode));
        
        return (await connection.QueryAsync<Customer>(
            "SELECT * FROM KhachHang WHERE MACN = @BranchCode",
            new { BranchCode = branchCode })).ToList();
    }
    // ... implement other methods
}
```

**Step 2**: Update DI registration in `AppBootstrapper.cs`:
```csharp
// OLD: In-memory implementation
// builder.RegisterType<CustomerService>()
//        .As<ICustomerService>()
//        .SingleInstance();

// NEW: Dapper implementation
builder.RegisterType<DapperCustomerService>()
       .As<ICustomerService>()
       .InstancePerDependency();
```

**Step 3**: No changes needed in:
- ✅ `BankDds.Core` (interfaces and models stay the same)
- ✅ `BankDds.Wpf` (ViewModels continue using `ICustomerService`)

This demonstrates the **Open/Closed Principle**: The system is open for extension (new implementations) but closed for modification (existing code unchanged).
## Features (Fully Implemented)

### Authentication & Authorization
- **Login Screen**: Branch selection + username/password authentication
- **Role-Based Access Control**: Three-tier permission system
  - **NganHang (Bank Level)**: Full access to all modules and all branches
  - **ChiNhanh (Branch Level)**: Access to assigned branch data only
  - **KhachHang (Customer)**: Read-only access to own accounts
- **Test Users**:
  - `admin` / `123` → Ngân Hàng (access to ALL)
  - `btuser` / `123` → Chi Nhánh BENTHANH
  - `tduser` / `123` → Chi Nhánh TANDINH
  - `c123456` / `123` → Khách Hàng (customer)
- **User Session**: Singleton service tracking username, role, branch, and permissions
- **Secure Logout**: Clears session and returns to login

### Customer Management (Full CRUD)
- ✅ **View Customers**: DataGrid with filtering by branch
- ✅ **Add Customer**: Form with validation
- ✅ **Edit Customer**: Inline editing with save/cancel
- ✅ **Delete Customer**: Remove customer records
- ✅ **Role-Based Filtering**: Branch users see only their branch customers

### Account Management
- ✅ **View Accounts**: List all accounts with balance
- ✅ **Filter by Branch**: Branch-specific data access
- ✅ **Filter by Customer**: View accounts for specific customers
- ✅ **Account Details**: SOTK, CMND, SODU, MACN, NGAYMOTK
- Ready for: Add/Edit/Delete operations

### Employee Management
- ✅ **View Employees**: List all employees
- ✅ **Branch Filtering**: See only same-branch employees
- ✅ **Employee Details**: MANV, HO, TEN, DIACHI, SDT, MACN
- Ready for: Add/Edit/Delete/Transfer operations

### Transaction Processing (Structure Ready)
- Ready for: Deposit (Gửi Tiền)
## Testing the Application

### Test as Bank Administrator
1. **Launch** the application
2. **Login** with:
   - Branch: `ALL`
   - Username: `admin`
   - Password: `123`
3. **Explore**:
   - All navigation buttons visible (Customers, Accounts, Employees, Transactions, Reports, Admin)
   - View data from all branches
   - Full CRUD operations in Customer module
4. **Logout** to return to login screen

## Code Conventions

### Caliburn.Micro Conventions
- **ViewModel naming**: `XxxViewModel` classes (e.g., `CustomersViewModel`)
- **View naming**: `XxxView` XAML files (e.g., `CustomersView.xaml`)
- **Binding conventions**: `x:Name` in XAML binds to properties/methods in ViewModel
- **Method binding**: Button `x:Name="Login"` calls `Login()` method
- **Property binding**: TextBox `x:Name="UserName"` binds to `UserName` property
- **Can-methods**: `CanLogin` property controls button enabled state
- **Conductor pattern**: `HomeViewModel` manages child screens using `Conductor<Screen>.Collection.OneActive`

### Dependency Injection (Autofac)
All services and ViewModels are registered in `AppBootstrapper.Configure()`:
- **Core Interfaces**: From `BankDds.Core.Interfaces` (ICustomerService, IAccountService, etc.)
- **Infrastructure Implementations**: From `BankDds.Infrastructure.Data` and `BankDds.Infrastructure.Security`
- **Services**: `SingleInstance` for in-memory data and session state (shared across app)
- **ViewModels**: `InstancePerDependency` (new instance for each navigation)

### Namespaces
- **Core Domain**: `BankDds.Core.Models`, `BankDds.Core.Interfaces`
- **Infrastructure**: `BankDds.Infrastructure.Data`, `BankDds.Infrastructure.Security`, `BankDds.Infrastructure.Configuration`
- **UI Layer**: `BankDds.Wpf.ViewModels`, `BankDds.Wpf.Views`, `BankDds.Wpf.Converters`
## Current Implementation Status

### ✅ Completed
- [x] Single-window architecture with Caliburn.Micro Conductor
- [x] Login with branch selection and authentication
- [x] Role-based authorization (NganHang, ChiNhanh, KhachHang)
- [x] User session management (IUserSession)
- [x] Navigation system with dynamic menu visibility
- [x] Customer management (full CRUD with DataGrid)
- [x] Account listing with role-based filtering
- [x] Employee listing with branch filtering
- [x] In-memory services with hard-coded data
- [x] Configuration system (appsettings.json)
- [x] All entity models (Customer, Account, Employee, Transaction, User)
- [x] Business services (6 services with full interfaces)
- [x] BoolToVisibilityConverter for UI binding

### 🚧 Ready for Enhancement
- [ ] Complete Account CRUD operations (add/edit/delete)
- [ ] Complete Employee CRUD operations (add/edit/delete/transfer)
- [ ] Transaction forms (Deposit/Withdraw/Transfer UI)
- [ ] Report generation UI (account statements, date range reports)
- [ ] Admin user management (add/edit/delete users)
- [ ] Input validation with error messages
- [ ] Confirm dialogs for delete operations

### 🔄 Next Phase: Database Integration
- [ ] Create Dapper implementations of service interfaces in `BankDds.Infrastructure/Data`
- [ ] Use distributed queries for cross-branch operations
- [ ] Implement stored procedures for complex transactions
- [ ] Use connection strings from `appsettings.json` (already configured)
- [ ] Update DI registration in `AppBootstrapper` to use new implementations
- [ ] **Zero changes needed** in `BankDds.Core` or `BankDds.Wpf` ✅
## Architecture Notes

### Single Window Architecture
The application uses **one window** (`MainShellView`) that contains all screens:
- `MainShellViewModel` (in `BankDds.Wpf.ViewModels`) is a `Conductor<Screen>.Collection.OneActive`
- Login screen loads first
- After login, switches to `HomeViewModel` (also a Conductor)
- `HomeViewModel` manages all feature modules (Customers, Accounts, etc.)
- Navigation changes content area, not windows

### User Session Management
`IUserSession` (defined in `BankDds.Core.Interfaces`, implemented in `BankDds.Infrastructure.Data`) maintains global session state:
- `Username` - The authenticated username
- `DisplayName` - User display name
- `UserGroup` - Enum (NganHang, ChiNhanh, KhachHang) from `BankDds.Core.Models`
- `SelectedBranch` - Current branch context
- `PermittedBranches` - List of accessible branches
- `CustomerCMND` - Customer ID (for customer users)
- `IsAuthenticated` - Login status

### Authentication Service
`IAuthService` (defined in `BankDds.Infrastructure.Security`) handles authentication:
- `LoginAsync(serverName, userName, password)` - Validates credentials
- `LogoutAsync()` - Cleanup
- Returns `AuthResult` with UserGroup and DefaultBranch
- Currently uses hard-coded users in `SqlAuthService` (4 test accounts)
- Ready for SQL Server authentication integration

### Service Layer
Six business service interfaces (defined in `BankDds.Core.Interfaces`) manage data operations:
- `ICustomerService` - Customer CRUD operations
- `IAccountService` - Account management
- `IEmployeeService` - Employee management (includes transfer)
- `ITransactionService` - Deposit/Withdraw/Transfer with balance validation
- `IReportService` - Generate various reports
- `IUserService` - User administration

**Current implementations** (in `BankDds.Infrastructure.Data`) use in-memory `List<T>` with hard-coded data.  
**Easy to replace** with Dapper/EF Core implementations without touching Core or UI layers.

### Role-Based Filtering
Data filtering happens in service implementations based on `IUserSession`:
```csharp
// In ViewModel (BankDds.Wpf)
if (_userSession.UserGroup == UserGroup.NganHang)
    customers = await _customerService.GetAllCustomersAsync();
else
    customers = await _customerService.GetCustomersByBranchAsync(_userSession.SelectedBranch);
```

UI menu visibility controlled by boolean properties:
```csharp
public bool CanViewAdmin => _userSession.UserGroup == UserGroup.NganHang;
```

### Configuration System
`appsettings.json` (in `BankDds.Wpf`) contains:
- Connection strings for each branch (BENTHANH, TANDINH)
- Bank main connection string
- Default branch setting

`IConnectionStringProvider` (interface in `BankDds.Core.Interfaces`, implementation in `BankDds.Infrastructure.Configuration`) provides connection strings by branch name.

### Why This Architecture Matters

**Before Refactoring** (2-layer):
```
BankDds.Wpf
├── Models/          ← Mixed domain + UI models
├── Services/        ← Business logic in UI project
└── ViewModels/      ← UI logic

BankDds.Infrastructure
└── Security/        ← Only authentication
```
❌ **Problems**: 
- Domain models coupled to UI project
- Business logic mixed with presentation logic
- Difficult to test business rules
- Hard to swap data access implementations

**After Refactoring** (3-layer clean architecture):
```
BankDds.Core         ← Pure domain, no dependencies
├── Models/
└── Interfaces/

BankDds.Infrastructure ← Depends only on Core
├── Data/
├── Security/
└── Configuration/

BankDds.Wpf          ← Depends on Core + Infrastructure
├── ViewModels/
├── Views/
└── Converters/
```
✅ **Benefits**:
- Domain models are pure and reusable
- Business logic is isolated and testable
- Data access can be swapped without UI changes
- Clear dependency direction (inward toward Core)
- Follows SOLID principles (especially Dependency Inversion)

## Architecture Highlights
- **3-Layer Clean Architecture**: Core (domain) → Infrastructure (data) → Wpf (UI)
- **MVVM Pattern**: Clean separation of presentation and business logic
- **Dependency Inversion**: UI and Infrastructure both depend on Core abstractions
- **Single Window Design**: All screens in one `MainShellView` container
- **Navigation System**: Conductor pattern for screen management
- **Caliburn.Micro Conventions**: Automatic view-viewmodel binding by name
- **Interface-Based Design**: All services accessed through Core interfaces
- **In-Memory Data**: Current implementation uses `List<T>` (ready for DB swap)

## How to Build and Run

### Prerequisites
- Visual Studio 2022 or later
- .NET 8 SDK

### Build
```cmd
cd d:\Projects\SV\CSDL-PT\BankDds.Wpf
dotnet restore
dotnet build
```

### Run
```cmd
dotnet run --project BankDds.Wpf\BankDds.Wpf.csproj
```

Or open `BankDds.Wpf.sln` in Visual Studio and press F5.

## License

This is a course project for distributed database systems.
