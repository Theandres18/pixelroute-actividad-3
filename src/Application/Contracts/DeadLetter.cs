namespace PixelRoute.Application.Contracts;

/// <summary>Mensaje que no se pudo entregar tras agotar los reintentos. No contiene datos personales.</summary>
public sealed record DeadLetter(
    Guid Id,
    string MessageType,
    string Payload,
    string Error,
    int Attempts,
    DateTimeOffset CreatedAt);
