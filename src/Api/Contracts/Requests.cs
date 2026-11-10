namespace PixelRoute.Api.Contracts;

/// <summary>DTO de entrada para crear una entrega.</summary>
public sealed record CreateDeliveryRequest(Guid OrderId, string? Address);

/// <summary>DTO de entrada para asignar un repartidor.</summary>
public sealed record AssignCourierRequest(Guid CourierId);
