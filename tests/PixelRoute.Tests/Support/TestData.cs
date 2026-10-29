using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Tests.Support;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 29, 10, 0, 0, TimeSpan.Zero);

    public static Delivery PendingDelivery()
        => new(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Calle 10 # 5-20, Barrio Centro"),
            DeliveryPromise.FromCreation(Now));

    public static CourierAvailability AvailableCourier(CourierId? id = null, int active = 0, int max = 3)
        => new(id ?? CourierId.New(), true, active, max);

    public static CourierAvailability OfflineCourier()
        => new(CourierId.New(), false, 0, 3);
}
