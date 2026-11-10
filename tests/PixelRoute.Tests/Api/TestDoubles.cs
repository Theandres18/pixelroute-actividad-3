using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using PixelRoute.Application.Abstractions;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Notifications;

namespace PixelRoute.Tests.Api;

/// <summary>Registra los mensajes de log para poder comprobar que no contienen datos sensibles.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger : ILogger
    {
        private readonly ConcurrentQueue<string> _messages;

        public CapturingLogger(ConcurrentQueue<string> messages) => _messages = messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Enqueue(formatter(state, exception));
        }
    }
}

/// <summary>Sustituye al sistema externo de notificaciones para provocar fallas a voluntad.</summary>
public sealed class ControllableNotificationGateway : INotificationGateway
{
    private int _calls;

    public bool Fail { get; set; }

    public int Calls => _calls;

    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _calls);

        if (Fail)
        {
            throw new NotificationUnavailableException("simulado");
        }

        return Task.CompletedTask;
    }
}

/// <summary>Repositorio que siempre falla, para comprobar que un error inesperado no filtra detalles internos.</summary>
public sealed class ThrowingDeliveryRepository : IDeliveryRepository
{
    public const string SecretMessage = "boom-secret-detail";

    public Task<Delivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(SecretMessage);

    public Task SaveAsync(Delivery delivery, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(SecretMessage);
}
