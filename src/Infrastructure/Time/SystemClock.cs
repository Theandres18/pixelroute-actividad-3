using PixelRoute.Application.Abstractions;

namespace PixelRoute.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
