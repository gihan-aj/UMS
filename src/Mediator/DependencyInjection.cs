using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Mediator
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Adds the core Mediator services (Mediator class, ISender, IPublisher)
        /// to the specified <see cref="IServiceCollection"/>.
        /// Optionally scans provided assemblies for *generic pipeline behaviors defined within those assemblies*.
        /// </summary>
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, params Assembly[] scanAssembliesForBehaviors)
        {
            services.AddScoped<Mediator>();
            services.AddScoped<ISender>(provider => provider.GetRequiredService<Mediator>());
            services.AddScoped<IPublisher>(provider => provider.GetRequiredService<Mediator>());

            // This part is for registering generic pipeline behaviors that might be part
            // of the UMS.Mediator library itself, or if you want this method to also
            // handle scanning for behaviors from other assemblies passed to it.
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
