using PixelRoute.Application.Contracts;
using PixelRoute.Application.Errors;
using PixelRoute.Application.UseCases;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Application;

/// <summary>
/// Pruebas del caso de uso solo con implementaciones falsas de los puertos (QS-15):
/// no necesitan base de datos, red ni servicios externos.
/// </summary>
public class AssignCourierUseCaseTests
{
    private readonly FakeDeliveryRepository _deliveries = new();
    private readonly FakeCourierAvailabilityProvider _couriers = new();
    private readonly SpyEventPublisher _events = new();
    private readonly FixedClock _clock = new(TestData.Now);

    private AssignCourierUseCase CreateUseCase()
        => new(_deliveries, _couriers, _events, _clock);

    // E-01 y E-08
    [Fact]
    public async Task Asigna_guarda_y_publica_el_evento()
    {
        var delivery = TestData.PendingDelivery();
        var courier = TestData.AvailableCourier();
        _deliveries.Add(delivery);
        _couriers.Add(courier);

        var result = await CreateUseCase().ExecuteAsync(
            new AssignCourierCommand(delivery.Id.Value, courier.CourierId.Value));

        Assert.Equal("Assigned", result.Status);
        Assert.Equal(courier.CourierId.Value, result.CourierId);
        Assert.Equal(TestData.Now, result.AssignedAt);
        Assert.Equal(1, _deliveries.SaveCount);
        Assert.Single(_events.Published);
        Assert.Empty(delivery.DomainEvents);
    }

    // E-05
    [Fact]
    public async Task Entrega_inexistente_lanza_DeliveryNotFoundException_y_no_guarda()
    {
        var courier = TestData.AvailableCourier();
        _couriers.Add(courier);

        await Assert.ThrowsAsync<DeliveryNotFoundException>(() => CreateUseCase().ExecuteAsync(
            new AssignCourierCommand(Guid.NewGuid(), courier.CourierId.Value)));

        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Empty(_events.Published);
    }

    // E-06
    [Fact]
    public async Task Repartidor_inexistente_lanza_CourierNotFoundException_y_no_guarda()
    {
        var delivery = TestData.PendingDelivery();
        _deliveries.Add(delivery);

        await Assert.ThrowsAsync<CourierNotFoundException>(() => CreateUseCase().ExecuteAsync(
            new AssignCourierCommand(delivery.Id.Value, Guid.NewGuid())));

        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Empty(_events.Published);
    }

    // E-04 y E-11
    [Fact]
    public async Task Ruta_saturada_lanza_excepcion_de_dominio_y_no_guarda_ni_publica()
    {
        var delivery = TestData.PendingDelivery();
        var saturated = TestData.AvailableCourier(active: 3, max: 3);
        _deliveries.Add(delivery);
        _couriers.Add(saturated);

        await Assert.ThrowsAsync<CourierRouteSaturatedException>(() => CreateUseCase().ExecuteAsync(
            new AssignCourierCommand(delivery.Id.Value, saturated.CourierId.Value)));

        Assert.Equal(0, _deliveries.SaveCount);
        Assert.Empty(_events.Published);
        Assert.Equal(DeliveryStatus.Pending, delivery.Status);
    }

    // E-02
    [Fact]
    public async Task Entrega_ya_asignada_lanza_DeliveryAlreadyAssignedException()
    {
        var delivery = TestData.PendingDelivery();
        var first = TestData.AvailableCourier();
        var second = TestData.AvailableCourier();
        _deliveries.Add(delivery);
        _couriers.Add(first);
        _couriers.Add(second);
        var useCase = CreateUseCase();

        await useCase.ExecuteAsync(new AssignCourierCommand(delivery.Id.Value, first.CourierId.Value));

        await Assert.ThrowsAsync<DeliveryAlreadyAssignedException>(() => useCase.ExecuteAsync(
            new AssignCourierCommand(delivery.Id.Value, second.CourierId.Value)));

        Assert.Equal(first.CourierId, delivery.AssignedCourierId);
    }
}
