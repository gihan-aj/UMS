using Microsoft.Extensions.DependencyInjection;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure.Persistence.Repositories;
using UMS.Infrastructure.Services;

namespace UMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            // Register the PasswordHasherService
            // It's stateless, so Transient or Scoped are fine. Singleton could also work.
            services.AddTransient<IPasswordHasherService, PasswordHasherService>();

            // Register the ReferenceCodeGeneratorService
            // This in-memory version can be a singleton for now as it uses a static ConcurrentDictionary.
            // A database-backed one would likely be Scoped or Transient depending on DbContext lifetime.
            services.AddSingleton<IReferenceCodeGeneratorService, ReferenceCodeGeneratorService>();

            // Register the InMemoryUserRepository
            // For an in-memory store that needs to persist data across requests in a web app,
            // Singleton is the appropriate lifetime. If it were a real DB context, Scoped would be used.
            services.AddSingleton<IUserRepository, InMemoryUserRepository>();
            // WHEN YOU MOVE TO A REAL DATABASE (e.g., with EF Core):
            // services.AddScoped<IUserRepository, YourEfCoreUserRepository>();
            // And you would also register your DbContext here:
            // services.AddDbContext<YourApplicationDbContext>(options =>
            //    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            return services;
        }
    }
}
