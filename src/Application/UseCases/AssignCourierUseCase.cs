using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;
using PixelRoute.Application.Errors;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.UseCases;

/// <summary>
/// Intención: asignar un repartidor a una entrega.
/// Coordina el flujo; las reglas del negocio viven en la entidad Delivery.
/// Recibe sus dependencias por constructor y solo depende de interfaces.
/// </summary>
public sealed class AssignCourierUseCase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly ICourierAvailabilityProvider _couriers;
    private readonly IDomainEventPublisher _events;
    private readonly IClock _clock;

    public AssignCourierUseCase(
        IDeliveryRepository deliveries,
        ICourierAvailabilityProvider couriers,
        IDomainEventPublisher events,
        IClock clock)
    {
        _deliveries = deliveries;
        _couriers = couriers;
        _events = events;
        _clock = clock;
    }

    public async Task<AssignCourierResult> ExecuteAsync(
        AssignCourierCommand command,
        CancellationToken cancellationToken = default)
    {
        var deliveryId = new DeliveryId(command.DeliveryId);
        var courierId = new CourierId(command.CourierId);

        // 1 y 2. Cargar la entrega y verificar que exista.
        var delivery = await _deliveries.GetAsync(deliveryId, cancellationToken)
            ?? throw new DeliveryNotFoundException(deliveryId);

        // 3. Consultar disponibilidad mediante un puerto, no mediante una API concreta.
        var availability = await _couriers.GetAvailabilityAsync(courierId, cancellationToken)
            ?? throw new CourierNotFoundException(courierId);

        // 4. La entidad protege las invariantes.
        delivery.AssignCourier(availability, _clock.UtcNow);

        // 5. Guardar mediante el repositorio.
        await _deliveries.SaveAsync(delivery, cancellationToken);

        // 6. Publicar o registrar el resultado mediante un adaptador.
        await _events.PublishAsync(delivery.DomainEvents, cancellationToken);
        delivery.ClearDomainEvents();

        return new AssignCourierResult(
            delivery.Id.Value,
            availability.CourierId.Value,
            delivery.Status.ToString(),
            delivery.AssignedAt!.Value);
    }
}
