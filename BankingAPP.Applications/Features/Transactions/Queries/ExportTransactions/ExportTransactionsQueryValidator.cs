using FluentValidation;
using System;

namespace BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions
{
    public class ExportTransactionsQueryValidator : AbstractValidator<ExportTransactionsQuery>
    {
        public ExportTransactionsQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty()
                .WithMessage("AccountId is required.");

            RuleFor(x => x.Format)
                .IsInEnum()
                .WithMessage("Invalid export format.");

            RuleFor(x => x)
                .Must(HaveValidDateRange)
                .WithMessage("FromDate cannot be later than ToDate.");
        }

        private bool HaveValidDateRange(ExportTransactionsQuery query)
        {
            if (query.FromDate.HasValue && query.ToDate.HasValue)
            {
                return query.FromDate.Value <= query.ToDate.Value;
            }
            return true;
        }
    }
}
