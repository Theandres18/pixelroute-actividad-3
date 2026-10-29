using System.Collections.Concurrent;
using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Couriers;

namespace PixelRoute.Infrastructure.Couriers;

/// <summary>Adaptador en memoria del puerto ICourierAvailabilityProvider. Más adelante se reemplaza por la fuente real de la flota.</summary>
public sealed class InMemoryCourierAvailabilityProvider : ICourierAvailabilityProvider
{
    private readonly ConcurrentDictionary<Guid, CourierAvailability> _items = new();

    public void Set(CourierAvailability availability) => _items[availability.CourierId.Value] = availability;

    public Task<CourierAvailability?> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(courierId.Value, out var availability);
        return Task.FromResult<CourierAvailability?>(availability);
    }
}
