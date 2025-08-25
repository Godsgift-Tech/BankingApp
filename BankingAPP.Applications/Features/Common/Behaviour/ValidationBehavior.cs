using FluentValidation;
using MediatR;
using ValidationException = BankingAPP.Applications.Features.Common.Exceptions.ValidationException;

namespace BankingAPP.Applications.Features.Common.Behaviour
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (_validators.Any())
            {
                var context = new ValidationContext<TRequest>(request);

                var failures = _validators
                    .Select(v => v.Validate(context))
                    .SelectMany(result => result.Errors)
                    .Where(f => f != null)
                    .Select(f => f.ErrorMessage)
                    .ToList();

                if (failures.Count != 0)
                {
                    //throwing custom validation exception to vallidation middleware(account Type validation)
                    throw new ValidationException(failures);
                }
            }

            return await next();
        }
    }
}
