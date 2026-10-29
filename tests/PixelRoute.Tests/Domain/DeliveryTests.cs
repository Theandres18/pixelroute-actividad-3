using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Domain;

public class DeliveryTests
{
    // E-01
    [Fact]
    public void AssignCourier_en_entrega_pendiente_asigna_repartidor_y_cambia_estado()
    {
        var delivery = TestData.PendingDelivery();
        var courier = TestData.AvailableCourier();

        delivery.AssignCourier(courier, TestData.Now);

        Assert.Equal(DeliveryStatus.Assigned, delivery.Status);
        Assert.Equal(courier.CourierId, delivery.AssignedCourierId);
        Assert.Equal(TestData.Now, delivery.AssignedAt);
    }

    // E-02
    [Fact]
    public void AssignCourier_dos_veces_lanza_DeliveryAlreadyAssignedException()
    {
        var delivery = TestData.PendingDelivery();
        delivery.AssignCourier(TestData.AvailableCourier(), TestData.Now);

        Assert.Throws<DeliveryAlreadyAssignedException>(
            () => delivery.AssignCourier(TestData.AvailableCourier(), TestData.Now));
    }

    // E-03
    [Fact]
    public void AssignCourier_con_repartidor_fuera_de_linea_lanza_CourierNotAvailableException()
    {
        var delivery = TestData.PendingDelivery();

        Assert.Throws<CourierNotAvailableException>(
            () => delivery.AssignCourier(TestData.OfflineCourier(), TestData.Now));

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Null(delivery.AssignedCourierId);
    }

    // E-04
    [Fact]
    public void AssignCourier_con_ruta_saturada_lanza_CourierRouteSaturatedException()
    {
        var delivery = TestData.PendingDelivery();
        var saturated = TestData.AvailableCourier(active: 3, max: 3);

        Assert.Throws<CourierRouteSaturatedException>(
            () => delivery.AssignCourier(saturated, TestData.Now));

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
    }

    // E-07
    [Fact]
    public void AssignCourier_en_entrega_cancelada_lanza_InvalidDeliveryStateException()
    {
        var delivery = TestData.PendingDelivery();
        delivery.Cancel();

        Assert.Throws<InvalidDeliveryStateException>(
            () => delivery.AssignCourier(TestData.AvailableCourier(), TestData.Now));
    }

    // E-08
    [Fact]
    public void AssignCourier_registra_el_evento_DeliveryAssigned()
    {
        var delivery = TestData.PendingDelivery();
        var courier = TestData.AvailableCourier();

        delivery.AssignCourier(courier, TestData.Now);

        var domainEvent = Assert.Single(delivery.DomainEvents);
        var assigned = Assert.IsType<DeliveryAssigned>(domainEvent);
        Assert.Equal(delivery.Id, assigned.DeliveryId);
        Assert.Equal(courier.CourierId, assigned.CourierId);
    }

    // E-09
    [Fact]
    public void ReleaseCourier_devuelve_la_entrega_a_pendiente_y_permite_reasignar()
    {
        var delivery = TestData.PendingDelivery();
        delivery.AssignCourier(TestData.AvailableCourier(), TestData.Now);

        delivery.ReleaseCourier();

        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
        Assert.Null(delivery.AssignedCourierId);

        var other = TestData.AvailableCourier();
        delivery.AssignCourier(other, TestData.Now);
        Assert.Equal(other.CourierId, delivery.AssignedCourierId);
    }

    // INV-08
    [Fact]
    public void La_promesa_no_cambia_al_asignar_el_repartidor()
    {
        var delivery = TestData.PendingDelivery();
        var promiseBefore = delivery.Promise;

        delivery.AssignCourier(TestData.AvailableCourier(), TestData.Now.AddMinutes(20));

        Assert.Equal(promiseBefore, delivery.Promise);
        Assert.Equal(TestData.Now.AddHours(3), delivery.Promise.PromisedBy);
    }

    // E-10
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DeliveryAddress_vacia_lanza_DomainException(string value)
    {
        Assert.Throws<DomainException>(() => new DeliveryAddress(value));
    }
}
