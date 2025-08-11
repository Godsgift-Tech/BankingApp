using FluentValidation;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Queries.ExportTransactions
{
    public class ExportTransactionsQueryValidator : AbstractValidator<ExportTransactionsQuery>
    {
        public ExportTransactionsQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("AccountId is required.");

            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("Account number is required.")
                .Length(10).WithMessage("Account number must be exactly 10 digits.");

            RuleFor(x => x.Format)
                .IsInEnum().WithMessage("Invalid export format specified.");

            RuleFor(x => x)
                .Custom((query, context) =>
                {
                    if (query.FromDate.HasValue && query.ToDate.HasValue &&
                        query.FromDate > query.ToDate)
                    {
                        Log.Warning("Invalid date range for export: FromDate {FromDate} is after ToDate {ToDate}",
                            query.FromDate, query.ToDate);
                        context.AddFailure("FromDate", "FromDate cannot be later than ToDate.");
                    }
                });
        }
    }
}
