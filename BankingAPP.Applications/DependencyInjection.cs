using BankingAPP.Applications.Features.Common.Behaviour.BankingAPP.Applications.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BankingAPP.Applications
{
    
        public static class DependencyInjection
        {
            public static IServiceCollection AddApplication(this IServiceCollection services)
            {
                // Register MediatR and all handlers in this assembly
                services.AddMediatR(cfg =>
                    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

                // Register FluentValidation validators
                services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

                // Register validation pipeline behavior
                services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

                return services;
            }
        }
    
}
