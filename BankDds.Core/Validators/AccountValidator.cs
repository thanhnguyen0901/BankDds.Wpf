using FluentValidation;
using BankDds.Core.Interfaces;

namespace BankDds.Core.Validators;

public class AccountValidator : AbstractValidator<Models.Account>
{
    public AccountValidator(IBranchRepository branchRepository)
    {
        // Account number validation - must be exactly 9 characters (nChar(9))
        RuleFor(x => x.SOTK)
            .NotEmpty().WithMessage("Số tài khoản (SOTK) là bắt buộc.")
            .Length(9).WithMessage("Số tài khoản phải đúng 9 ký tự.")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Số tài khoản chỉ được chứa chữ in hoa và chữ số.");

        // Customer CMND validation: exactly 10 digits (nChar(10) in DB â€” GAP-08)
        RuleFor(x => x.CMND)
            .NotEmpty().WithMessage("CMND khách hàng là bắt buộc.")
            .Length(10).WithMessage("CMND phải đúng 10 chữ số.")
            .Matches(@"^\d+$").WithMessage("CMND chỉ được chứa chữ số.");

        // Balance validation: Must be >= 0
        RuleFor(x => x.SODU)
            .GreaterThanOrEqualTo(0).WithMessage("Số dư (SODU) không được âm.");

        // Branch code validation: required and must exist in the branch repository
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Mã chi nhánh (MACN) là bắt buộc.")
            .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
            .WithMessage("Mã chi nhánh không tồn tại trong hệ thống.");

        // Opening date validation: Cannot be in the future
        RuleFor(x => x.NGAYMOTK)
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày mở tài khoản không được lớn hơn ngày hiện tại.");
    }
}

