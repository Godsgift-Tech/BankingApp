using FluentValidation;
using System;

namespace BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions
{
    public class ExportTransactionsQueryValidator : AbstractValidator<ExportTransactionsQuery>
    {
        public ExportTransactionsQueryValidator()
        {
            // At least one of AccountId or AccountNumber must be provided
            RuleFor(x => x)
                .Must(x => (x.AccountId.HasValue && x.AccountId != Guid.Empty)
                           || !string.IsNullOrWhiteSpace(x.AccountNumber))
                .WithMessage("Either AccountId or AccountNumber must be provided.");

            //  Ensure format is valid
            RuleFor(x => x.Format)
                .IsInEnum()
                .WithMessage("Invalid export format.");

            // Ensure date range is valid
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
