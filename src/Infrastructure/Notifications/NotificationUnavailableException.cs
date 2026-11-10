namespace PixelRoute.Infrastructure.Notifications;

public sealed class NotificationUnavailableException : Exception
{
    public NotificationUnavailableException(string message) : base(message)
    {
    }
}
