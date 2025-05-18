using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mediator
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, params Assembly[] scanAssembliesForBehaviors)
        {
            services.AddScoped<Mediator>();
            services.AddScoped<ISender>(provider => provider.GetRequiredService<Mediator>());

            // Register open generic pipeline behaviors if any assemblies are provided for scanning.
            // This is for generic behaviors defined within the assemblies passed to this method.
            if (scanAssembliesForBehaviors != null && scanAssembliesForBehaviors.Length > 0)
            {
                services.Scan(scan => scan
                    .FromAssemblies(scanAssembliesForBehaviors)
                    .AddClasses(classes => classes.AssignableTo(typeof(IPipelineBehavior<,>)))
                    .AsImplementedInterfaces() // Registers them as IPipelineBehavior<TRequest, TResponse>
                    .WithTransientLifetime()); // Behaviors are often transient.
            }
            // Note: Specific, non-generic behaviors or behaviors with dependencies
            // might still need manual registration or more specific scanning.

            return services;
        }
    }
}
