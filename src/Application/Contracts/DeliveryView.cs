namespace PixelRoute.Application.Contracts;

/// <summary>Vista de consulta de una entrega. No incluye la dirección, para minimizar la exposición de datos personales.</summary>
public sealed record DeliveryView(
    Guid DeliveryId,
    Guid OrderId,
    string Status,
    Guid? AssignedCourierId,
    DateTimeOffset? AssignedAt,
    DateTimeOffset PromisedBy);
