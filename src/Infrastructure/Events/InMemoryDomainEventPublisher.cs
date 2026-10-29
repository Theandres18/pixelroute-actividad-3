using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Common;

namespace PixelRoute.Infrastructure.Events;

/// <summary>Adaptador que registra los eventos en memoria. Útil para pruebas de integración del flujo.</summary>
public sealed class InMemoryDomainEventPublisher : IDomainEventPublisher
{
    private readonly List<IDomainEvent> _published = new();

    public IReadOnlyList<IDomainEvent> Published => _published;

    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        _published.AddRange(events);
        return Task.CompletedTask;
    }
}
