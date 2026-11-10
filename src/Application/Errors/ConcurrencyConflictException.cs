using PixelRoute.Domain.Deliveries;

namespace PixelRoute.Application.Errors;

/// <summary>La entrega fue modificada por otra operación entre su lectura y su guardado.</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(DeliveryId deliveryId)
        : base($"La entrega {deliveryId} fue modificada por otra operación. Vuelva a consultarla e intente de nuevo.")
    {
    }
}
