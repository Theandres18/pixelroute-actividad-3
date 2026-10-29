using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;

namespace PixelRoute.Domain.Deliveries;

/// <summary>
/// Entidad raíz del agregado Entrega. Protege las invariantes de la asignación de repartidor.
/// No conoce bases de datos, APIs ni frameworks.
/// </summary>
public sealed class Delivery
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Delivery(DeliveryId id, OrderId orderId, DeliveryAddress address, DeliveryPromise promise)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        OrderId = orderId ?? throw new ArgumentNullException(nameof(orderId));
        Address = address ?? throw new ArgumentNullException(nameof(address));
        Promise = promise ?? throw new ArgumentNullException(nameof(promise));
        Status = DeliveryStatus.Pending;
    }

    public DeliveryId Id { get; }

    public OrderId OrderId { get; }

    public DeliveryAddress Address { get; }

    public DeliveryPromise Promise { get; }

    public DeliveryStatus Status { get; private set; }

    public CourierId? AssignedCourierId { get; private set; }

    public DateTimeOffset? AssignedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Asigna un repartidor. Protege INV-01, INV-02, INV-04 e INV-05.</summary>
    public void AssignCourier(CourierAvailability courier, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(courier);

        // INV-01: una entrega tiene como máximo un repartidor activo.
        if (AssignedCourierId is not null)
        {
            throw new DeliveryAlreadyAssignedException(Id, AssignedCourierId);
        }

        // INV-02: solo una entrega pendiente puede asignarse.
        if (Status != DeliveryStatus.Pending)
        {
            throw new InvalidDeliveryStateException(Id, Status, "asignar un repartidor a");
        }

        // INV-04: no se asigna a un repartidor fuera de línea.
        if (!courier.IsOnline)
        {
            throw new CourierNotAvailableException(courier.CourierId);
        }

        // INV-05: no se asigna a un repartidor con la ruta saturada.
        if (courier.IsRouteSaturated)
        {
            throw new CourierRouteSaturatedException(courier.CourierId, courier.MaxActiveDeliveries);
        }

        AssignedCourierId = courier.CourierId;
        AssignedAt = now;
        Status = DeliveryStatus.Assigned;

        _domainEvents.Add(new DeliveryAssigned(Id, courier.CourierId, now));
    }

    /// <summary>Libera al repartidor (por ejemplo, si rechaza la entrega). Vuelve a Pendiente.</summary>
    public void ReleaseCourier()
    {
        if (Status != DeliveryStatus.Assigned)
        {
            throw new InvalidDeliveryStateException(Id, Status, "liberar al repartidor de");
        }

        AssignedCourierId = null;
        AssignedAt = null;
        Status = DeliveryStatus.Pending;
    }

    /// <summary>Cancela la entrega. Solo es posible si está Pendiente o Asignada (INV-06).</summary>
    public void Cancel()
    {
        if (Status != DeliveryStatus.Pending && Status != DeliveryStatus.Assigned)
        {
            throw new InvalidDeliveryStateException(Id, Status, "cancelar");
        }

        AssignedCourierId = null;
        AssignedAt = null;
        Status = DeliveryStatus.Cancelled;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
