using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;

namespace PixelRoute.Domain.Deliveries;

public sealed class DeliveryAlreadyAssignedException : DomainException
{
    public DeliveryAlreadyAssignedException(DeliveryId deliveryId, CourierId courierId)
        : base($"La entrega {deliveryId} ya tiene asignado al repartidor {courierId}.")
    {
    }
}

public sealed class InvalidDeliveryStateException : DomainException
{
    public InvalidDeliveryStateException(DeliveryId deliveryId, DeliveryStatus status, string action)
        : base($"No se puede {action} la entrega {deliveryId} porque está en estado {status}.")
    {
    }
}

public sealed class CourierNotAvailableException : DomainException
{
    public CourierNotAvailableException(CourierId courierId)
        : base($"El repartidor {courierId} no está disponible (fuera de línea).")
    {
    }
}

public sealed class CourierRouteSaturatedException : DomainException
{
    public CourierRouteSaturatedException(CourierId courierId, int maxActiveDeliveries)
        : base($"La ruta del repartidor {courierId} está saturada (máximo {maxActiveDeliveries} entregas activas).")
    {
    }
}
