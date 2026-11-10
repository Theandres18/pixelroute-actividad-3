using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.UseCases;

/// <summary>Intención: crear una entrega Pendiente a partir de un pedido listo para despacho.</summary>
public sealed class CreateDeliveryUseCase
{
    private readonly IDeliveryRepository _deliveries;
    private readonly IClock _clock;

    public CreateDeliveryUseCase(IDeliveryRepository deliveries, IClock clock)
    {
        _deliveries = deliveries;
        _clock = clock;
    }

    public async Task<CreateDeliveryResult> ExecuteAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        var delivery = new Delivery(
            DeliveryId.New(),
            new OrderId(command.OrderId),
            new DeliveryAddress(command.Address),
            DeliveryPromise.FromCreation(_clock.UtcNow));

        await _deliveries.SaveAsync(delivery, cancellationToken);

        return new CreateDeliveryResult(
            delivery.Id.Value,
            delivery.Status.ToString(),
            delivery.Promise.PromisedBy);
    }
}
