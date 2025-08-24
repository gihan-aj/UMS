using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Application.Settings;
using UMS.Domain.Users;
using UMS.Infrastructure.Authentication;
using UMS.Infrastructure.Authentication.Settings;
using UMS.Infrastructure.Authorization;
using UMS.Infrastructure.BackgroundJobs;
using UMS.Infrastructure.Persistence;
using UMS.Infrastructure.Persistence.Interceptors;
using UMS.Infrastructure.Persistence.Repositories;
using UMS.Infrastructure.Persistence.Seeders;
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
            services.AddScoped<DispatchDomainEventsInterceptor>();

            // --- Database Context Registration ---
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                // Resolve the interceptor from the service provider
                var interceptor = sp.GetRequiredService<DispatchDomainEventsInterceptor>();
                var connectionString = configuration.GetConnectionString("DefaultConnection");

                if(connectionString!.Contains("Host=", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseNpgsql(connectionString, npgSqlOptions =>
                    {
                        npgSqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                        npgSqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                }
                else
                {
                    options.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
                    {
                        /**
                        * Specify the assembly where migrations are located.
                        * This tells EF Core to look for migrations in the UMS.Infrastructure assembly.
                        * When you run Add-Migration, ensure UMS.Infrastructure is the default project.
                        * EF Core will then create/use a "Migrations" folder within this project structure.
                        * To place it under Persistence/Migrations, ensure your DbContext is in Persistence
                        * or manage the folder structure manually after generation if needed, though
                        * EF Core typically creates "Migrations" at the root of the migrationsAssembly.
                        * For better control, you can specify the output directory for migrations
                        * via the -OutputDir parameter in Add-Migration command, e.g.,
                        * Add-Migration InitialCreate -OutputDir Persistence/Migrations
                        */
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName); // More robust
                        // Or: sqlOptions.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);

                        // Enable retry on failure, useful for transient connection issues (e.g., Azure SQL)
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);

                        // Configure split queries to avoid performance warnings
                        // and improve efficiency for queries with multiple .Include() on collections.
                        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                }

                options.AddInterceptors(interceptor);
            });

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
            services.Configure<AdminSettings>(configuration.GetSection(AdminSettings.SectionName));
            services.Configure<SmtpSettings>(configuration.GetSection(SmtpSettings.SectionName));

            // Register the JWT token generator service
            services.AddScoped<IJwtTokenGeneratorService, JwtTokenGeneratorService>();
            // Singleton is fine for JwtTokenGeneratorService as it's stateless and configured via IOptions<JwtSettings>

            // --- Authorization Services ---
            services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            // --- Email Service ---
            services.AddTransient<IEmailService, MailKitEmailService>();

            // Register the PasswordHasherService
            // It's stateless, so Transient or Scoped are fine. Singleton could also work.
            services.AddTransient<IPasswordHasherService, PasswordHasherService>();

            // Register the ReferenceCodeGeneratorService
            // This in-memory version can be a singleton for now as it uses a static ConcurrentDictionary.
            // A database-backed one would likely be Scoped or Transient depending on DbContext lifetime.
            services.AddScoped<IReferenceCodeGeneratorService, ReferenceCodeGeneratorService>();
            services.AddScoped<ISequenceGeneratorService, SequenceGeneratorService>();

            // --- Repository Implementations ---
            // Replace InMemoryUserRepository with EfCoreUserRepository.
            // Repositories using a Scoped DbContext should also be Scoped.
            services.AddScoped<IUserRepository, EfCoreUserRepository>();
            services.AddScoped<IRoleRepository, EfCoreRoleRepository>();
            services.AddScoped<IPermissionRepository, EfCorePermissionRepository>();
            services.AddScoped<IClientRepository, EfCoreClientRepository>();

            // --- Background Job Registration ---
            // Register the cleanup job as a hosted service
            services.AddHostedService<CleanupOldRefreshTokensJob>();

            // --- Seeder Registration ---
            services.AddScoped<DatabaseSeeder>();

            // --- Custom IdentityServer Services ---
            services.AddTransient<IResourceOwnerPasswordValidator, ResourceOwnerPasswordValidator>();
            services.AddTransient<IProfileService, ProfileService>();

            // --- IdentityServer Setup ---
            services.AddIdentityServer(options =>
                {
                    options.KeyManagement.Enabled = false;
                })
                .AddInMemoryApiResources(IdentityServerConfig.GetApiResources())
                .AddInMemoryApiScopes(IdentityServerConfig.GetApiScopes())
                .AddInMemoryClients(IdentityServerConfig.GetClients())
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = b =>
                        b.UseNpgsql(configuration.GetConnectionString("DefaultConnection"),
                            sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

                })
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
                .AddProfileService<ProfileService>()
                .AddDeveloperSigningCredential();

            return services;
        }

        // Extension method to run the seeder
        public static async Task UseInfrastructureServicesAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;

            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            // This is the programmatic equivalent of running "Update-Database".
            await dbContext.Database.MigrateAsync();

            var seeder = services.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
    }
}
