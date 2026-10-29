using PixelRoute.Application.Contracts;
using PixelRoute.Application.UseCases;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Couriers;
using PixelRoute.Infrastructure.Events;
using PixelRoute.Infrastructure.Persistence;
using PixelRoute.Infrastructure.Time;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Integration;

/// <summary>Prueba del flujo completo con los adaptadores reales (en memoria) de la capa Infrastructure.</summary>
public class AssignCourierFlowTests
{
    [Fact]
    public async Task Flujo_completo_asigna_persiste_y_publica_con_adaptadores_en_memoria()
    {
        var deliveries = new InMemoryDeliveryRepository();
        var couriers = new InMemoryCourierAvailabilityProvider();
        var events = new InMemoryDomainEventPublisher();
        var useCase = new AssignCourierUseCase(deliveries, couriers, events, new SystemClock());

        var delivery = TestData.PendingDelivery();
        var courier = TestData.AvailableCourier(active: 1, max: 3);
        deliveries.Add(delivery);
        couriers.Set(courier);

        var result = await useCase.ExecuteAsync(
            new AssignCourierCommand(delivery.Id.Value, courier.CourierId.Value));

        var saved = await deliveries.GetAsync(delivery.Id);
        Assert.NotNull(saved);
        Assert.Equal(DeliveryStatus.Assigned, saved!.Status);
        Assert.Equal(courier.CourierId, saved.AssignedCourierId);
        Assert.Equal("Assigned", result.Status);

        var published = Assert.Single(events.Published);
        Assert.IsType<DeliveryAssigned>(published);
    }
}
