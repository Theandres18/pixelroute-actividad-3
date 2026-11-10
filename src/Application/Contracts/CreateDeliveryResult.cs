namespace PixelRoute.Application.Contracts;

public sealed record CreateDeliveryResult(Guid DeliveryId, string Status, DateTimeOffset PromisedBy);
