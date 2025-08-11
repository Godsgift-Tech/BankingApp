using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Accounts.Commands.CreateAccount
{
    public class CreateAccountCommandValidator : AbstractValidator<CreateAccountCommand>
    {
        public CreateAccountCommandValidator()
        {
            RuleFor(x => x.AccountType)
                .NotEmpty().WithMessage("Account type is required.")
                .Must(type => type == "Savings" || type == "Current")
                .WithMessage("Account type must be either 'Savings' or 'Current'.");

            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Currency is required.")
                .Length(3).WithMessage("Currency must be a valid 3-letter code (e.g., NGN, USD).");
        }
    }

}
