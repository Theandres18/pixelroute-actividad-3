namespace PixelRoute.Domain.Deliveries;

public sealed record DeliveryId(Guid Value)
{
    public static DeliveryId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
