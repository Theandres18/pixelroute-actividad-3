namespace PixelRoute.Domain.Couriers;

/// <summary>
/// Objeto de valor: foto de la disponibilidad de un repartidor en un momento dado.
/// Lo entrega un puerto; el dominio no sabe de dónde viene el dato.
/// </summary>
public sealed record CourierAvailability(
    CourierId CourierId,
    bool IsOnline,
    int ActiveDeliveries,
    int MaxActiveDeliveries)
{
    /// <summary>La ruta está saturada cuando ya lleva el máximo de entregas activas.</summary>
    public bool IsRouteSaturated => ActiveDeliveries >= MaxActiveDeliveries;
}
