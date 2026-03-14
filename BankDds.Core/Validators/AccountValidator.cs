using BankDds.Core.Interfaces;
using FluentValidation;

namespace BankDds.Core.Validators
{
    /// <summary>
    /// Validates account input before account creation or account update operations.
    /// </summary>
    public class AccountValidator : AbstractValidator<Models.Account>
    {
        public AccountValidator(IBranchRepository branchRepository)
        {
            // Logic: SOTK is the business key used by transactions and replication, so it must be strict and fixed length.
            RuleFor(x => x.SOTK)
                .NotEmpty().WithMessage("Số tài khoản (SOTK) là bắt buộc.")
                .Length(9).WithMessage("Số tài khoản phải đúng 9 ký tự.")
                .Matches(@"^[A-Z0-9]+$").WithMessage("Số tài khoản chỉ được chứa chữ in hoa và chữ số.");

            // Logic: CMND links account ownership to a specific customer profile.
            RuleFor(x => x.CMND)
                .NotEmpty().WithMessage("CMND khách hàng là bắt buộc.")
                .Length(10).WithMessage("CMND phải đúng 10 chữ số.")
                .Matches(@"^\d+$").WithMessage("CMND chỉ được chứa chữ số.");

            // Logic: Balance cannot be negative in the current business flow.
            RuleFor(x => x.SODU)
                .GreaterThanOrEqualTo(0).WithMessage("Số dư (SODU) không được âm.");

            // Logic: Account must belong to a valid branch to keep branch-scoped authorization consistent.
            RuleFor(x => x.MACN)
                .NotEmpty().WithMessage("Mã chi nhánh (MACN) là bắt buộc.")
                .MustAsync((macn, ct) => branchRepository.BranchExistsAsync(macn))
                .WithMessage("Mã chi nhánh không tồn tại trong hệ thống.");

            // Logic: Open date cannot be in the future.
            RuleFor(x => x.NGAYMOTK)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("Ngày mở tài khoản không được lớn hơn ngày hiện tại.");
        }
    }
}
