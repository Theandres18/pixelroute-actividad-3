using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;

namespace PixelRoute.Application.UseCases;

/// <summary>Intención: que operaciones revise los mensajes que no pudieron entregarse.</summary>
public sealed class ListDeadLettersUseCase
{
    private const int MaxLimit = 200;

    private readonly IDeadLetterStore _deadLetters;

    public ListDeadLettersUseCase(IDeadLetterStore deadLetters)
    {
        _deadLetters = deadLetters;
    }

    public Task<IReadOnlyList<DeadLetter>> ExecuteAsync(int limit, CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, MaxLimit);
        return _deadLetters.ListAsync(safeLimit, cancellationToken);
    }
}
