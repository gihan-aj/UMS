using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Settings;
using UMS.Infrastructure.Authentication.Settings;
using UMS.Infrastructure.BackgroundJobs;
using UMS.Infrastructure.Persistence;
using UMS.Infrastructure.Persistence.Repositories;
using UMS.Infrastructure.Services;
using UMS.Infrastructure.Settings;

namespace UMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // --- Database Context Registration ---
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                    // Optional: configure SQL Server specific options
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        // Specify the assembly where migrations are located.
                        // This tells EF Core to look for migrations in the UMS.Infrastructure assembly.
                        // When you run Add-Migration, ensure UMS.Infrastructure is the default project.
                        // EF Core will then create/use a "Migrations" folder within this project structure.
                        // To place it under Persistence/Migrations, ensure your DbContext is in Persistence
                        // or manage the folder structure manually after generation if needed, though
                        // EF Core typically creates "Migrations" at the root of the migrationsAssembly.
                        // For better control, you can specify the output directory for migrations
                        // via the -OutputDir parameter in Add-Migration command, e.g.,
                        // Add-Migration InitialCreate -OutputDir Persistence/Migrations
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName); // More robust
                        // Or: sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);

                        // Enable retry on failure, useful for transient connection issues (e.g., Azure SQL)
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            // --- Unit of Work Registration ---
            // UnitOfWork depends on ApplicationDbContext, so it should have a similar lifetime (Scoped).
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // --- Authentication/Authorization Settings & Services ---
            // Bind JwtSettings from appsettings.json to the JwtSettings class
            // --- Settings Registration ---
            services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
            services.Configure<TokenSettings>(configuration.GetSection(TokenSettings.SectionName));
            services.Configure<ClientAppSettings>(configuration.GetSection(ClientAppSettings.SectionName));
            services.Configure<CleanupSettings>(configuration.GetSection(CleanupSettings.SectionName));

            // Register the JWT token generator service
            services.AddSingleton<IJwtTokenGeneratorService, JwtTokenGeneratorService>();
            // Singleton is fine for JwtTokenGeneratorService as it's stateless and configured via IOptions<JwtSettings>

            // --- Email Service ---
            // Register the dummy console email service.
            // When you want to send real emails, you'll replace ConsoleEmailService
            // with your actual implementation (e.g., SendGridEmailService).
            services.AddTransient<IEmailService, ConsoleEmailService>();

            // Register the PasswordHasherService
            // It's stateless, so Transient or Scoped are fine. Singleton could also work.
            services.AddTransient<IPasswordHasherService, PasswordHasherService>();

            // Register the ReferenceCodeGeneratorService
            // This in-memory version can be a singleton for now as it uses a static ConcurrentDictionary.
            // A database-backed one would likely be Scoped or Transient depending on DbContext lifetime.
            services.AddScoped<IReferenceCodeGeneratorService, ReferenceCodeGeneratorService>();

            // --- Repository Implementations ---
            // Replace InMemoryUserRepository with EfCoreUserRepository.
            // Repositories using a Scoped DbContext should also be Scoped.
            services.AddScoped<IUserRepository, EfCoreUserRepository>();

            // --- Background Job Registration ---
            // Register the cleanup job as a hosted service
            services.AddHostedService<CleanupOldRefreshTokensJob>();

            return services;
        }
    }
}
