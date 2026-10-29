using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Common;

namespace PixelRoute.Infrastructure.Events;

/// <summary>Segundo adaptador del mismo puerto: escribe los eventos en la consola. Demuestra que el adaptador es intercambiable.</summary>
public sealed class ConsoleDomainEventPublisher : IDomainEventPublisher
{
    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            Console.WriteLine($"[{domainEvent.OccurredAt:O}] {domainEvent.GetType().Name}: {domainEvent}");
        }

        return Task.CompletedTask;
    }
}
