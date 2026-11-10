using PixelRoute.Application.Contracts;
using PixelRoute.Application.Errors;
using PixelRoute.Application.UseCases;
using PixelRoute.Domain.Common;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Application;

public class CreateAndGetDeliveryUseCaseTests
{
    private readonly FakeDeliveryRepository _deliveries = new();
    private readonly FixedClock _clock = new(TestData.Now);

    [Fact]
    public async Task Crear_guarda_una_entrega_pendiente_con_promesa_de_3_horas()
    {
        var useCase = new CreateDeliveryUseCase(_deliveries, _clock);

        var result = await useCase.ExecuteAsync(new CreateDeliveryCommand(Guid.NewGuid(), "Calle 10 # 5-20"));

        Assert.Equal("Pending", result.Status);
        Assert.Equal(TestData.Now.AddHours(3), result.PromisedBy);
        Assert.Equal(1, _deliveries.SaveCount);
    }

    [Fact]
    public async Task Crear_con_direccion_vacia_lanza_DomainException_y_no_guarda()
    {
        var useCase = new CreateDeliveryUseCase(_deliveries, _clock);

        await Assert.ThrowsAsync<DomainException>(
            () => useCase.ExecuteAsync(new CreateDeliveryCommand(Guid.NewGuid(), "   ")));

        Assert.Equal(0, _deliveries.SaveCount);
    }

    [Fact]
    public async Task Consultar_devuelve_la_vista_sin_la_direccion()
    {
        var delivery = TestData.PendingDelivery();
        _deliveries.Add(delivery);

        var view = await new GetDeliveryUseCase(_deliveries).ExecuteAsync(delivery.Id.Value);

        Assert.Equal(delivery.Id.Value, view.DeliveryId);
        Assert.Equal("Pending", view.Status);
        Assert.Null(view.AssignedCourierId);
        Assert.DoesNotContain(typeof(DeliveryView).GetProperties(), property => property.Name == "Address");
    }

    [Fact]
    public async Task Consultar_una_entrega_inexistente_lanza_DeliveryNotFoundException()
    {
        await Assert.ThrowsAsync<DeliveryNotFoundException>(
            () => new GetDeliveryUseCase(_deliveries).ExecuteAsync(Guid.NewGuid()));
    }
}
