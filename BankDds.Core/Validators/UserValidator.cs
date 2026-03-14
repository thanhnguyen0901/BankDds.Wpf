using FluentValidation;

namespace BankDds.Core.Validators;

public class UserValidator : AbstractValidator<Models.User>
{
    public UserValidator()
    {
        // SQL login name validation
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

        // In SQL-login mode this field carries the plain password input for SP calls.
        RuleFor(x => x.PasswordHash)
            .NotEmpty().WithMessage("Password is required")
            .Unless((user, ctx) => ctx.RootContextData.ContainsKey("SkipPasswordValidation"));

        // NGUOIDUNG.DefaultBranch is mandatory and must be a real branch code.
        RuleFor(x => x.DefaultBranch)
            .NotEmpty().WithMessage("Default branch is required")
            .Must(x => x == "BENTHANH" || x == "TANDINH")
            .WithMessage("Default branch must be 'BENTHANH' or 'TANDINH'");

        // CustomerCMND is mandatory for KHACHHANG accounts.
        RuleFor(x => x.CustomerCMND)
            .NotEmpty().WithMessage("Customer CMND is required for customer users")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang);

        RuleFor(x => x.CustomerCMND)
            .Length(10).WithMessage("Customer CMND must be exactly 10 digits")
            .Matches(@"^\d+$").WithMessage("Customer CMND must contain only numeric digits")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang && !string.IsNullOrEmpty(x.CustomerCMND));

        // EmployeeId is mandatory for CHINHANH accounts.
        RuleFor(x => x.EmployeeId)
            .NotEmpty().WithMessage("Employee ID is required for branch users")
            .When(x => x.UserGroup == Models.UserGroup.ChiNhanh);

        RuleFor(x => x.EmployeeId)
            .Length(10).WithMessage("Employee ID must be exactly 10 characters (NVxxxxxxxx)")
            .Matches(@"^NV\d{8}$").WithMessage("Employee ID format must be NVxxxxxxxx")
            .When(x => x.UserGroup == Models.UserGroup.ChiNhanh && !string.IsNullOrWhiteSpace(x.EmployeeId));
    }
}
