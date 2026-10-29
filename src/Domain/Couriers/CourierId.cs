namespace PixelRoute.Domain.Couriers;

public sealed record CourierId(Guid Value)
{
    public static CourierId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
