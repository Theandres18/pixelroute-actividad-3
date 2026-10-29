using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.Errors;

public sealed class DeliveryNotFoundException : Exception
{
    public DeliveryNotFoundException(DeliveryId deliveryId)
        : base($"No existe la entrega {deliveryId}.")
    {
    }
}

public sealed class CourierNotFoundException : Exception
{
    public CourierNotFoundException(CourierId courierId)
        : base($"No existe el repartidor {courierId}.")
    {
    }
}
