using System.Globalization;
using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;

namespace PixelRoute.Infrastructure.Persistence;

/// <summary>Adaptador SQLite de la cola de mensajes fallidos.</summary>
public sealed class SqliteDeadLetterStore : IDeadLetterStore
{
    private readonly SqliteConnectionFactory _factory;

    public SqliteDeadLetterStore(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO dead_letter_messages (id, message_type, payload, error, attempts, created_at) " +
            "VALUES (@id, @type, @payload, @error, @attempts, @createdAt)";
        command.Parameters.AddWithValue("@id", deadLetter.Id.ToString());
        command.Parameters.AddWithValue("@type", deadLetter.MessageType);
        command.Parameters.AddWithValue("@payload", deadLetter.Payload);
        command.Parameters.AddWithValue("@error", deadLetter.Error);
        command.Parameters.AddWithValue("@attempts", deadLetter.Attempts);
        command.Parameters.AddWithValue("@createdAt", deadLetter.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DeadLetter>> ListAsync(int limit, CancellationToken cancellationToken = default)
    {
        var result = new List<DeadLetter>();

        await using var connection = await _factory.OpenAsync(cancellationToken);
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT id, message_type, payload, error, attempts, created_at " +
            "FROM dead_letter_messages ORDER BY created_at DESC LIMIT @limit";
        command.Parameters.AddWithValue("@limit", limit);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new DeadLetter(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetInt32(4),
                DateTimeOffset.Parse(reader.GetString(5), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)));
        }

        return result;
    }
}
