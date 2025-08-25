using FluentValidation;

namespace BankingAPP.Applications.Features.Transactions.Commands.Withdraw
{
    public class WithdrawCommandValidator : AbstractValidator<WithdrawCommand>
    {
        public WithdrawCommandValidator()
        {
            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.")
                .Length(10).WithMessage("Account number must be 10 digits."); // adjust if needed

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Withdrawal amount must be greater than zero.");

            RuleFor(x => x.Description)
                .MaximumLength(250).WithMessage("Description must not exceed 250 characters.");
        }
    }
}
