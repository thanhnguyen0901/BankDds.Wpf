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

        // Default branch is used by service-layer branch checks.
        // Optional for listing rows loaded from sp_DanhSachNhanVien.
        RuleFor(x => x.DefaultBranch)
            .Must(x => string.IsNullOrWhiteSpace(x) || x == "BENTHANH" || x == "TANDINH")
            .WithMessage("Default branch must be empty, 'BENTHANH', or 'TANDINH'");

        // CustomerCMND is only mandatory for KHACHHANG creation flows.
        RuleFor(x => x.CustomerCMND)
            .NotEmpty().WithMessage("Customer CMND is required for customer users")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang);

        RuleFor(x => x.CustomerCMND)
            .Length(9, 12).WithMessage("Customer CMND must be 9-12 digits")
            .Matches(@"^\d+$").WithMessage("Customer CMND must contain only numeric digits")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang && !string.IsNullOrEmpty(x.CustomerCMND));

        // EmployeeId is optional in SQL-login mode; validate format only when provided.
        RuleFor(x => x.EmployeeId)
            .Length(10).WithMessage("Employee ID must be exactly 10 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.EmployeeId));
    }
}
