using PixelRoute.Domain.Couriers;

namespace PixelRoute.Application.Abstractions;

/// <summary>Puerto de salida: consulta de disponibilidad de repartidores (sin atarse a una API concreta).</summary>
public interface ICourierAvailabilityProvider
{
    Task<CourierAvailability?> GetAvailabilityAsync(CourierId courierId, CancellationToken cancellationToken = default);
}
