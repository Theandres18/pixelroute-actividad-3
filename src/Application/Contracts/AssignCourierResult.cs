namespace PixelRoute.Application.Contracts;

/// <summary>Contrato de salida del caso de uso.</summary>
public sealed record AssignCourierResult(
    Guid DeliveryId,
    Guid CourierId,
    string Status,
    DateTimeOffset AssignedAt);
