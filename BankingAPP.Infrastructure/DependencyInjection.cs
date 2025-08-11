using BankingApp.Core.Entities;
using BankingAPP.Applications.Features.Common.Interfaces;
using BankingAPP.Infrastructure.Data;
using BankingAPP.Infrastructure.Repositories;
using BankingAPP.Infrastructure.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using StackExchange.Redis;

namespace BankingAPP.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddDbContext<BankingDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Identity
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<BankingDbContext>()
                .AddDefaultTokenProviders();
            //Services
            services.AddScoped<IExportService, ExportService>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            //  services.AddScoped<IExportService, ExportService>();
            // services.AddScoped<ITransactionRepository, TransactionRepository>();


            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var config = ConfigurationOptions.Parse("localhost:6379", true);
                return ConnectionMultiplexer.Connect(config);
            });

            services.AddScoped<IDatabase>(sp =>
            {
                var connection = sp.GetRequiredService<IConnectionMultiplexer>();
                return connection.GetDatabase();
            });


            // Redis (native StackExchange.Redis)
            var redisConn = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConn));

                services.AddSingleton<IDatabase>(sp =>
                {
                    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    return multiplexer.GetDatabase();
                });

                Log.Information("StackExchange.Redis configured: {RedisConnection}", redisConn);
            }
            else
            {
                Log.Warning("Redis connection string is missing, StackExchange.Redis will not be registered.");
            }

            return services;
        }
    }
}
