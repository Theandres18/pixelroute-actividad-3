using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;
using PixelRoute.Application.Errors;
using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.UseCases;

/// <summary>Intención: consultar el estado de una entrega.</summary>
public sealed class GetDeliveryUseCase
{
    private readonly IDeliveryRepository _deliveries;

    public GetDeliveryUseCase(IDeliveryRepository deliveries)
    {
        _deliveries = deliveries;
    }

    public async Task<DeliveryView> ExecuteAsync(Guid deliveryId, CancellationToken cancellationToken = default)
    {
        var id = new DeliveryId(deliveryId);
        var delivery = await _deliveries.GetAsync(id, cancellationToken)
            ?? throw new DeliveryNotFoundException(id);

        return new DeliveryView(
            delivery.Id.Value,
            delivery.OrderId.Value,
            delivery.Status.ToString(),
            delivery.AssignedCourierId?.Value,
            delivery.AssignedAt,
            delivery.Promise.PromisedBy);
    }
}
