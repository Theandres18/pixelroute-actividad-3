using System.Collections.Concurrent;
using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Infrastructure.Persistence;

/// <summary>Adaptador en memoria del puerto IDeliveryRepository. La versión con base de datos real llega en la Actividad 4.</summary>
public sealed class InMemoryDeliveryRepository : IDeliveryRepository
{
    private readonly ConcurrentDictionary<Guid, Delivery> _items = new();

    public void Add(Delivery delivery) => _items[delivery.Id.Value] = delivery;

    public Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var delivery);
        return Task.FromResult<Delivery?>(delivery);
    }

    public Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        _items[delivery.Id.Value] = delivery;
        return Task.CompletedTask;
    }
}
