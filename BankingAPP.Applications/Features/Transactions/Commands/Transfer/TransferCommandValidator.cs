using FluentValidation;

namespace BankingAPP.Applications.Features.Transactions.Commands.Transfer
{
    public class TransferCommandValidator : AbstractValidator<TransferCommand>
    {
        public TransferCommandValidator()
        {
            RuleFor(x => x.FromAccountNumber)
                .NotEmpty().WithMessage("Source account number is required.")
                .Length(10).WithMessage("Source account number must be 10 digits."); 

            RuleFor(x => x.ToAccountNumber)
                .NotEmpty().WithMessage("Target account number is required.")
                .Length(10).WithMessage("Target account number must be 10 digits."); 

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Transfer amount must be greater than zero.") // the minimum amount
                .LessThanOrEqualTo(10_000_000).WithMessage("Transfer amount exceeds the allowed limit."); // upper limit

            RuleFor(x => x.Description)
                .MaximumLength(250).WithMessage("Description cannot be longer than 250 characters.");
        }
    }
}
