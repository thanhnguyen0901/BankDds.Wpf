using FluentValidation;
using BankDds.Core.Interfaces;

namespace BankDds.Core.Validators;

public class EmployeeValidator : AbstractValidator<Models.Employee>
{
    public EmployeeValidator(IBranchRepository branchRepository)
    {
        RuleFor(x => x.MANV)
            .NotEmpty().WithMessage("Mã nhân viên (MANV) là bắt buộc.")
            .Length(10).WithMessage("Mã nhân viên phải đúng 10 ký tự.");

        RuleFor(x => x.HO)
            .NotEmpty().WithMessage("Họ là bắt buộc.")
            .MaximumLength(50).WithMessage("Họ không được vượt quá 50 ký tự.");

        RuleFor(x => x.TEN)
            .NotEmpty().WithMessage("Tên là bắt buộc.")
            .MaximumLength(10).WithMessage("Tên không được vượt quá 10 ký tự.");

        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND là bắt buộc.")
            .Length(10).WithMessage("CMND phải đúng 10 chữ số.")
            .Matches(@"^\d+$").WithMessage("CMND chỉ được chứa chữ số.");

        RuleFor(x => x.SODT)
            .Matches(@"^(0\d{9,10})$").WithMessage("Số điện thoại phải có 10-11 chữ số và bắt đầu bằng 0.")
            .When(x => !string.IsNullOrEmpty(x.SODT));

        RuleFor(x => x.PHAI)
            .NotEmpty().WithMessage("Giới tính là bắt buộc.")
            .Must(x => x == "Nam" || x == "Nữ").WithMessage("Giới tính phải là 'Nam' hoặc 'Nữ'.");

        RuleFor(x => x.DIACHI)
            .NotEmpty().WithMessage("Địa chỉ là bắt buộc.")
            .MaximumLength(100).WithMessage("Địa chỉ không được vượt quá 100 ký tự.");

        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Mã chi nhánh (MACN) là bắt buộc.")
            .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
            .WithMessage("Mã chi nhánh không tồn tại trong hệ thống.");
    }
}
