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

            // Services
            services.AddScoped<IExportService, ExportService>();

            // Repositories
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();

            // Redis (native StackExchange.Redis)
            var redisConn = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConn))
            {
                // Register native Redis connection
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(redisConn));

                services.AddSingleton<IDatabase>(sp =>
                {
                    var multiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
                    return multiplexer.GetDatabase();
                });

                // Register IDistributedCache with Redis
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConn;
                });

                Log.Information("StackExchange.Redis configured: {RedisConnection}", redisConn);
            }
            else
            {
                // Fallback to in-memory cache
                services.AddDistributedMemoryCache();
                Log.Warning("Redis connection string is missing, using in-memory cache instead.");
            }

            return services;
        }
    }
}
