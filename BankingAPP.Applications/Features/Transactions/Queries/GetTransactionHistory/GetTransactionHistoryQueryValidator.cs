using FluentValidation;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankingAPP.Applications.Features.Transactions.Queries.GetTransactionHistory
{
    public class GetTransactionHistoryQueryValidator : AbstractValidator<GetTransactionHistoryQuery>
    {
        public GetTransactionHistoryQueryValidator()
        {
            RuleFor(x => x.AccountId)
                .NotEmpty().WithMessage("AccountId is required.")
                .Must(id => id != Guid.Empty)
                .WithMessage("AccountId must be a valid GUID.");

            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("PageNumber must be greater than zero.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("PageSize must be greater than zero.")
                .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");

            // validation for date range
            RuleFor(x => x)
                .Custom((query, context) =>
                {
                    if (query.FromDate.HasValue && query.ToDate.HasValue &&
                        query.FromDate.Value > query.ToDate.Value)
                    {
                        context.AddFailure("fromDate", "fromDate must be earlier than or equal to toDate.");
                    }

                    // log suspicious inputs  here)
                    if (query.PageNumber <= 0 || query.PageSize <= 0)
                    {
                        Log.Warning("Invalid transaction history query for AccountId: {AccountId} (page:{Page}, size:{Size})",
                            query.AccountId, query.PageNumber, query.PageSize);
                    }
                });
        }
    }
}
