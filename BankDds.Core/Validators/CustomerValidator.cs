using BankDds.Core.Interfaces;
using FluentValidation;

namespace BankDds.Core.Validators
{
    /// <summary>
    /// Validates customer profile input for branch customer management workflows.
    /// </summary>
    public class CustomerValidator : AbstractValidator<Models.Customer>
    {
        public CustomerValidator(IBranchRepository branchRepository)
        {
            // Logic: CMND is the unique customer identity used across accounts and login mapping.
            RuleFor(x => x.CMND)
                .NotEmpty().WithMessage("CMND là bắt buộc.")
                .Length(10).WithMessage("CMND phải đúng 10 chữ số.")
                .Matches(@"^\d+$").WithMessage("CMND chỉ được chứa chữ số.");

            RuleFor(x => x.Ho)
                .NotEmpty().WithMessage("Họ là bắt buộc.")
                .MaximumLength(50).WithMessage("Họ không được vượt quá 50 ký tự.");

            RuleFor(x => x.Ten)
                .NotEmpty().WithMessage("Tên là bắt buộc.")
                .MaximumLength(10).WithMessage("Tên không được vượt quá 10 ký tự.");

            // Logic: Birth date is mandatory and constrained to a realistic age range.
            RuleFor(x => x.NgaySinh)
                .NotNull().WithMessage("Ngày sinh là bắt buộc.")
                .LessThan(DateTime.Today).WithMessage("Ngày sinh phải nhỏ hơn ngày hiện tại.")
                .GreaterThan(DateTime.Today.AddYears(-150)).WithMessage("Ngày sinh phải nằm trong 150 năm gần nhất.");

            // Logic: ID issuance date must be valid and after date of birth.
            RuleFor(x => x.NgayCap)
                .NotNull().WithMessage("Ngày cấp CMND là bắt buộc.");

            RuleFor(x => x.NgayCap)
                .LessThanOrEqualTo(DateTime.Today).WithMessage("Ngày cấp CMND không được lớn hơn ngày hiện tại.")
                .When(x => x.NgayCap.HasValue);

            RuleFor(x => x.NgayCap)
                .GreaterThan(x => x.NgaySinh ?? DateTime.MinValue).WithMessage("Ngày cấp CMND phải sau ngày sinh.")
                .When(x => x.NgayCap.HasValue && x.NgaySinh.HasValue);

            RuleFor(x => x.SODT)
                .Matches(@"^(0\d{9,10})$").WithMessage("Số điện thoại phải có 10-11 chữ số và bắt đầu bằng 0.")
                .When(x => !string.IsNullOrEmpty(x.SODT));

            RuleFor(x => x.Phai)
                .NotEmpty().WithMessage("Giới tính là bắt buộc.")
                .Must(x => x == "Nam" || x == "Nữ").WithMessage("Giới tính phải là 'Nam' hoặc 'Nữ'.");

            RuleFor(x => x.DiaChi)
                .NotEmpty().WithMessage("Địa chỉ là bắt buộc.")
                .MaximumLength(100).WithMessage("Địa chỉ không được vượt quá 100 ký tự.");

            // Logic: A customer must always belong to an existing home branch.
            RuleFor(x => x.MaCN)
                .NotEmpty().WithMessage("Mã chi nhánh (MaCN) là bắt buộc.")
                .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
                .WithMessage("Mã chi nhánh không tồn tại trong hệ thống.");
        }
    }
}
