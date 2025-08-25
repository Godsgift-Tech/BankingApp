using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Commands.Deposit
{
    public class DepositCommandValidator : AbstractValidator<DepositCommand>
    {
        public DepositCommandValidator()
        {
            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.");

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Deposit amount must be greater than zero.");

            RuleFor(x => x.Description)
                .MaximumLength(250).WithMessage("Description cannot exceed 250 characters.");
        }
    }
}
