namespace PixelRoute.Domain.Deliveries;

/// <summary>Objeto de valor: hora límite prometida al cliente (3 horas desde la creación).</summary>
public sealed record DeliveryPromise(DateTimeOffset PromisedBy)
{
    public static readonly TimeSpan StandardWindow = TimeSpan.FromHours(3);

    public static DeliveryPromise FromCreation(DateTimeOffset createdAt)
        => new(createdAt + StandardWindow);
}
