using FluentValidation;

namespace BankDds.Core.Validators;

public class UserValidator : AbstractValidator<Models.User>
{
    public UserValidator()
    {
        // Username validation: Required, 3-50 chars, alphanumeric and underscores
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Username can only contain letters, numbers, and underscores");

        // PasswordHash validation (only validate that it exists for new users)
        // Skip this validation when updating existing users without password change
        // The caller sets RootContextData["SkipPasswordValidation"] = true for edit-without-password-change.
        RuleFor(x => x.PasswordHash)
            .NotEmpty().WithMessage("Password is required")
            .Unless((user, ctx) => ctx.RootContextData.ContainsKey("SkipPasswordValidation"));

        // Default branch validation
        RuleFor(x => x.DefaultBranch)
            .NotEmpty().WithMessage("Default branch is required")
            .Must(x => x == "BENTHANH" || x == "TANDINH" || x == "ALL")
            .WithMessage("Default branch must be 'BENTHANH', 'TANDINH', or 'ALL'");

        // CustomerCMND validation: Required if UserGroup is KhachHang
        RuleFor(x => x.CustomerCMND)
            .NotEmpty().WithMessage("Customer CMND is required for customer users")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang);

        RuleFor(x => x.CustomerCMND)
            .Length(9, 12).WithMessage("Customer CMND must be 9-12 digits")
            .Matches(@"^\d+$").WithMessage("Customer CMND must contain only numeric digits")
            .When(x => x.UserGroup == Models.UserGroup.KhachHang && !string.IsNullOrEmpty(x.CustomerCMND));

        // EmployeeId validation: Should be provided for non-customer users - exactly 10 characters
        RuleFor(x => x.EmployeeId)
            .NotNull().WithMessage("Employee ID is required for bank and branch users")
            .Length(10).WithMessage("Employee ID must be exactly 10 characters")
            .When(x => x.UserGroup == Models.UserGroup.NganHang || x.UserGroup == Models.UserGroup.ChiNhanh);
    }
}
