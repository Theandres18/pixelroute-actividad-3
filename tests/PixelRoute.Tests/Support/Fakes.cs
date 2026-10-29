using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Tests.Support;

// Implementaciones falsas de los puertos: permiten probar el caso de uso sin base de datos ni servicios externos.

internal sealed class FakeDeliveryRepository : IDeliveryRepository
{
    private readonly Dictionary<Guid, Delivery> _items = new();

    public int SaveCount { get; private set; }

    public void Add(Delivery delivery) => _items[delivery.Id.Value] = delivery;

    public Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(id.Value, out var delivery);
        return Task.FromResult<Delivery?>(delivery);
    }

    public Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
    {
        SaveCount++;
        _items[delivery.Id.Value] = delivery;
        return Task.CompletedTask;
    }
}

internal sealed class FakeCourierAvailabilityProvider : ICourierAvailabilityProvider
{
    private readonly Dictionary<Guid, CourierAvailability> _items = new();

    public void Add(CourierAvailability availability) => _items[availability.CourierId.Value] = availability;

    public Task<CourierAvailability?> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default)
    {
        _items.TryGetValue(courierId.Value, out var availability);
        return Task.FromResult<CourierAvailability?>(availability);
    }
}

internal sealed class SpyEventPublisher : IDomainEventPublisher
{
    public List<IDomainEvent> Published { get; } = new();

    public Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        Published.AddRange(events);
        return Task.CompletedTask;
    }
}

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; }
}
