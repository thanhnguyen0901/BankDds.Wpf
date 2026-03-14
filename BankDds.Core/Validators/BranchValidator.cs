using BankDds.Core.Models;
using FluentValidation;

namespace BankDds.Core.Validators;

public class BranchValidator : AbstractValidator<Branch>
{
    public BranchValidator()
    {
        // MACN: required, max 10 chars, uppercase alphanumeric (nChar(10) in DB)
        RuleFor(x => x.MACN)
            .NotEmpty().WithMessage("Mã chi nhánh (MACN) là bắt buộc.")
            .MaximumLength(10).WithMessage("Mã chi nhánh không được vượt quá 10 ký tự.")
            .Matches(@"^[A-Z0-9]+$").WithMessage("Mã chi nhánh chỉ được chứa chữ in hoa hoặc chữ số.");

        // TENCN: required, max 50 chars
        RuleFor(x => x.TENCN)
            .NotEmpty().WithMessage("Tên chi nhánh (TENCN) là bắt buộc.")
            .MaximumLength(50).WithMessage("Tên chi nhánh không được vượt quá 50 ký tự.");

        // DiaChi: optional, max 100 chars
        RuleFor(x => x.DiaChi)
            .MaximumLength(100).WithMessage("Địa chỉ không được vượt quá 100 ký tự.");

        // SODT: optional; if provided must start with 0 and be 10â€“15 digits
        RuleFor(x => x.SODT)
            .MaximumLength(15).WithMessage("Số điện thoại không được vượt quá 15 ký tự.")
            .Matches(@"^0\d{9,14}$").WithMessage("Số điện thoại phải bắt đầu bằng 0 và có từ 10 đến 15 chữ số.")
            .When(x => !string.IsNullOrEmpty(x.SODT));
    }
}

