using FluentValidation;

namespace BankDds.Core.Validators
{
    /// <summary>
    /// Validates user login setup based on role-specific identity requirements.
    /// </summary>
    public class UserValidator : AbstractValidator<Models.User>
    {
        public UserValidator()
        {
            // Logic: Username is a stable login key used by SQL authentication and NGUOIDUNG mapping.
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Tên đăng nhập là bắt buộc.")
                .Length(3, 50).WithMessage("Tên đăng nhập phải có từ 3 đến 50 ký tự.")
                .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Tên đăng nhập chỉ được chứa chữ cái, chữ số và dấu gạch dưới.");

            // Logic: Password can be skipped only in update scenarios when caller explicitly sets the context flag.
            RuleFor(x => x.PasswordHash)
                .NotEmpty().WithMessage("Mật khẩu là bắt buộc.")
                .Unless((user, ctx) => ctx.RootContextData.ContainsKey("SkipPasswordValidation"));

            // Logic: Current deployment supports two working branches.
            RuleFor(x => x.DefaultBranch)
                .NotEmpty().WithMessage("Chi nhánh mặc định là bắt buộc.")
                .Must(x => x == "BENTHANH" || x == "TANDINH")
                .WithMessage("Chi nhánh mặc định phải là 'BENTHANH' hoặc 'TANDINH'.");

            // Logic: KhachHang account must map to a customer identity.
            RuleFor(x => x.CustomerCMND)
                .NotEmpty().WithMessage("CMND khách hàng là bắt buộc với tài khoản nhóm KhachHang.")
                .When(x => x.UserGroup == Models.UserGroup.KhachHang);

            RuleFor(x => x.CustomerCMND)
                .Length(10).WithMessage("CMND khách hàng phải đúng 10 chữ số.")
                .Matches(@"^\d+$").WithMessage("CMND khách hàng chỉ được chứa chữ số.")
                .When(x => x.UserGroup == Models.UserGroup.KhachHang && !string.IsNullOrEmpty(x.CustomerCMND));

            // Logic: ChiNhanh account must map to an employee identity.
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Mã nhân viên là bắt buộc với tài khoản nhóm ChiNhanh.")
                .When(x => x.UserGroup == Models.UserGroup.ChiNhanh);

            RuleFor(x => x.EmployeeId)
                .Length(10).WithMessage("Mã nhân viên phải đúng 10 ký tự (NVxxxxxxxx).")
                .Matches(@"^NV\d{8}$").WithMessage("Định dạng mã nhân viên phải là NVxxxxxxxx.")
                .When(x => x.UserGroup == Models.UserGroup.ChiNhanh && !string.IsNullOrWhiteSpace(x.EmployeeId));
        }
    }
}
