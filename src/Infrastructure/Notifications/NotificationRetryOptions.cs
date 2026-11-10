namespace PixelRoute.Infrastructure.Notifications;

public sealed class NotificationRetryOptions
{
    /// <summary>Intentos totales de envío (el primero cuenta como intento).</summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>Espera base entre intentos; se duplica en cada reintento (200, 400, 800...).</summary>
    public int BaseDelayMilliseconds { get; init; } = 200;
}
