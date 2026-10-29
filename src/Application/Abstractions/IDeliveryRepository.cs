using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.Abstractions;

/// <summary>Puerto de salida: persistencia de entregas. Lo define la aplicación, lo implementa la infraestructura.</summary>
public interface IDeliveryRepository
{
    Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default);

    Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default);
}
