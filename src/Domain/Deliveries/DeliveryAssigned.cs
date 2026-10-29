using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;

namespace PixelRoute.Domain.Deliveries;

/// <summary>Evento de dominio: a una entrega se le asignó un repartidor.</summary>
public sealed record DeliveryAssigned(
    DeliveryId DeliveryId,
    CourierId CourierId,
    DateTimeOffset OccurredAt) : IDomainEvent;
