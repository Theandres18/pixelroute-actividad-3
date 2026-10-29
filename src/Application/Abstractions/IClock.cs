namespace PixelRoute.Application.Abstractions;

/// <summary>Puerto de salida: reloj. Permite probar el tiempo sin depender de la hora real.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
