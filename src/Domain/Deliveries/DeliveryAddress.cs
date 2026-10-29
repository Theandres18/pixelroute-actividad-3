using PixelRoute.Domain.Common;

namespace PixelRoute.Domain.Deliveries;

/// <summary>Objeto de valor: dirección de entrega. Invariante: no puede estar vacía.</summary>
public sealed record DeliveryAddress
{
    public DeliveryAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("La dirección de entrega es obligatoria.");
        }

        Value = value.Trim();
    }

    public string Value { get; }
}
