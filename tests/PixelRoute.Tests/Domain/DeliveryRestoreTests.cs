using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Domain;

/// <summary>La reconstrucción desde la base de datos también protege INV-03 (estado y repartidor coherentes).</summary>
public class DeliveryRestoreTests
{
    private static Delivery Restore(DeliveryStatus status, CourierId? courier, int version = 3)
    {
        DateTimeOffset? assignedAt = courier is null ? null : TestData.Now;

        return Delivery.Restore(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress("Calle 1 # 2-3"),
            DeliveryPromise.FromCreation(TestData.Now),
            status,
            courier,
            assignedAt,
            version);
    }

    [Fact]
    public void Restore_de_una_entrega_asignada_conserva_estado_repartidor_y_version()
    {
        var courier = CourierId.New();

        var delivery = Restore(DeliveryStatus.Assigned, courier);

        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(courier, delivery.AssignedCourierId);
        Assert.Equal(3, delivery.Version);
        Assert.Empty(delivery.DomainEvents);
    }

    [Fact]
    public void Restore_de_una_entrega_pendiente_con_repartidor_lanza_DomainException()
    {
        Assert.Throws<DomainException>(() => Restore(DeliveryStatus.Pending, CourierId.New()));
    }

    [Fact]
    public void Restore_de_una_entrega_asignada_sin_repartidor_lanza_DomainException()
    {
        Assert.Throws<DomainException>(() => Restore(DeliveryStatus.Assigned, null));
    }
}
