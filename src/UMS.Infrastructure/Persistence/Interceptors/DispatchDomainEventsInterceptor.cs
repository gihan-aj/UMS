using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using UMS.Domain.Primitives;

namespace UMS.Infrastructure.Persistence.Interceptors
{
    public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
    {
        private readonly IPublisher _publisher;

        public DispatchDomainEventsInterceptor(IPublisher publisher)
        {
            _publisher = publisher;
        }

        // This method is called by EF Core right before SaveChangesAsync is executed.
        // We gather the events here, but they won't be dispatched until AFTER the base method completes.
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await DispatchAndClearEvents(eventData.Context, cancellationToken);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        // This is the synchronous version.
        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            DispatchAndClearEvents(eventData.Context).GetAwaiter().GetResult();
            return base.SavingChanges(eventData, result);
        }

        private async Task DispatchAndClearEvents(DbContext? context, CancellationToken cancellationToken = default)
        {
            if (context is null) return;

            var aggregateRoots = context.ChangeTracker
                .Entries<IAggregateRoot>()
                .Where(e => e.Entity.GetDomainEvents().Any())
                .Select(e => e.Entity)
                .ToList();

            var domainEvents = aggregateRoots
                .SelectMany(e => e.GetDomainEvents())
                .ToList();

            // Clear events from the aggregates immediately.
            aggregateRoots.ForEach(e => e.ClearDomainEvents());

            // Dispatch the events
            foreach (var domainEvent in domainEvents)
            {
                await _publisher.Publish(domainEvent, cancellationToken);
            }
        }
    }
}