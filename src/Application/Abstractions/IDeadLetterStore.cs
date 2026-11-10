using PixelRoute.Application.Contracts;

namespace PixelRoute.Application.Abstractions;

/// <summary>Puerto de salida: almacén de mensajes que no pudieron entregarse (cola de mensajes fallidos).</summary>
public interface IDeadLetterStore
{
    Task AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeadLetter>> ListAsync(int limit, CancellationToken cancellationToken = default);
}
