using BankingApp.Core.Entities;
using BankingAPP.Infrastructure.Data;
using BankingAPP.Infrastructure.Identity;
using BankingAPP.Infrastructure.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuestPDF.Infrastructure;
using Serilog;
using System.Text;
using FluentValidation;
using BankingAPP.Applications;
using BankingAPP.Applications.Features.Common.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
    //.WriteTo.Seq("http://localhost:5341") // optional SEQ support
    .Enrich.FromLogContext()
    .MinimumLevel.Information()
    .CreateLogger();

try
{
    Log.Information("Starting BankingApp API...");

    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();

    // QuestPDF license
    QuestPDF.Settings.License = LicenseType.Community;

    var configuration = builder.Configuration;
    Console.WriteLine("Connection string in use: " + configuration.GetConnectionString("DefaultConnection"));

    // Database
    builder.Services.AddDbContext<BankingDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

   
    //  MediatR & FluentValidation
    builder.Services.AddMediatR(typeof(AssemblyMarker).Assembly);
    builder.Services.AddValidatorsFromAssemblyContaining(typeof(AssemblyMarker));


    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<BankingDbContext>()
        .AddDefaultTokenProviders();

    // Dependency Injection
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    //builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    //builder.Services.AddScoped<IExportService, ExportService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Redis Caching 
    try
    {
        var redisConn = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConn))
        {
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
            });

            Log.Information("Redis cache configured: {RedisConnection}", redisConn);
        }
        else
        {
            Log.Warning("Redis connection string is missing in appsettings.json, caching will be disabled.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to configure Redis. Caching will be disabled.");
    }

    // Swagger + JWT Auth
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Banking App API",
            Version = "v1"
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: **Bearer {your JWT token}**"
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // JWT Auth
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
        };
    });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    // Seed roles and admin user before handling requests
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await RoleSeeder.SeedRolesAsync(services);
        await BankAdminRole.SeedAdminUserAsync(services);
    }

    // Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseRouting();
    app.UseDeveloperExceptionPage();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Redirect root URL to Swagger
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
}
finally
{
    Log.CloseAndFlush();
}
