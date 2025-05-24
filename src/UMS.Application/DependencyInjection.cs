using System.Reflection;
using FluentValidation;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using UMS.Application.Common.Behaviors;
using UMS.Application.Features.Users.NotificationHandlers;
using UMS.Application.Features.Users.Notifications;

namespace UMS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            var applicationAssembly = Assembly.GetExecutingAssembly();

            // 1. Register your custom Mediator (ISender, IPublisher)
            // This call assumes AddCustomMediator from your UMS.Mediator project
            // is responsible for registering the Mediator class, ISender, and IPublisher.
            // The AddCustomMediator method should NOT be responsible for scanning application handlers.
            services.AddCustomMediator(); // Pass assemblies only if AddCustomMediator scans for *its own* generic behaviors

            // 2. Register FluentValidation services and validators from this assembly
            services.AddValidatorsFromAssembly(applicationAssembly);

            // 3. Register all IRequestHandler<,> (and thus ICommandHandler<,>, IQueryHandler<,>)
            //    and INotificationHandler<> implementations from this assembly (UMS.Application)
            services.Scan(scan => scan
                .FromAssemblies(applicationAssembly)
                // Register all classes implementing IRequestHandler<TRequest, TResponse>
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                    .AsImplementedInterfaces() // Registers them as IRequestHandler<TRequest, TResponse>
                    .WithTransientLifetime()   // Handlers are typically transient

                // Register all classes implementing IRequestHandler<TRequest> (for commands/requests without a value in TResponse)
                .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<>)))
                    .AsImplementedInterfaces() // Registers them as IRequestHandler<TRequest>
                    .WithTransientLifetime()

                .AddClasses(classes => classes.AssignableTo(typeof(INotificationHandler<>))) // Ensure this line exists and uses the correct INotificationHandler interface
                    .AsImplementedInterfaces()
                    .WithTransientLifetime()); 

            // 4. Register Pipeline Behaviors
            // These are application-specific pipeline behaviors.
            // If LoggingBehavior and ValidationPipelineBehavior are generic and defined in UMS.Application,
            // they can be registered as open generics.
            // The order of registration for pipeline behaviors can be important.
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

            // If you prefer explicit registration or if the scan isn't picking them up:
            services.AddTransient<INotificationHandler<UserCreatedNotification>, SendWelcomeEmailOnUserCreatedHandler>();
            services.AddTransient<INotificationHandler<UserCreatedNotification>, LogUserActivityOnUserCreatedHandler>();

            return services;
        }
    }
}
