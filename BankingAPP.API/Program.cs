using BankingApp.Application.Interfaces.Repository;
using BankingApp.Application.Interfaces.Services;
using BankingApp.Application.Services;
using BankingApp.Core.Entities;
using BankingAPP.Infrastructure.Data;
using BankingAPP.Infrastructure.Identity;
using BankingAPP.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using QuestPDF.Infrastructure; // ✅ Added for QuestPDF license

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

    // ✅ Set QuestPDF license
    QuestPDF.Settings.License = LicenseType.Community;

    var configuration = builder.Configuration;
    Console.WriteLine("Connection string in use: " + configuration.GetConnectionString("DefaultConnection"));

    // Database
    builder.Services.AddDbContext<BankingDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<BankingDbContext>()
        .AddDefaultTokenProviders();

    // Dependency Injection
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAccountRepository, AccountRepository>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
    builder.Services.AddScoped<ITransactionService, TransactionService>();
    builder.Services.AddScoped<IExportService, ExportService>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

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

    // Seed roles before the app starts handling requests
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

    // Optional: disable HTTPS redirection for now to avoid certificate issues
    // app.UseHttpsRedirection();

    app.UseRouting();
    app.UseDeveloperExceptionPage();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Redirect root URL to Swagger without showing in Swagger docs
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
