namespace PixelRoute.Application.Contracts;

public sealed record CreateDeliveryCommand(Guid OrderId, string Address);
