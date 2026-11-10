using Microsoft.Extensions.Logging.Abstractions;
using PixelRoute.Application.Abstractions;
using PixelRoute.Application.Contracts;
using PixelRoute.Domain.Common;
using PixelRoute.Domain.Couriers;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Notifications;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Infrastructure;

public class ResilientNotificationEventPublisherTests
{
    private sealed class FlakyGateway : INotificationGateway
    {
        private readonly int _failures;

        public FlakyGateway(int failures) => _failures = failures;

        public int Calls { get; private set; }

        public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Calls <= _failures)
            {
                throw new NotificationUnavailableException("simulado");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MemoryDeadLetterStore : IDeadLetterStore
    {
        public List<DeadLetter> Items { get; } = new();

        public Task AddAsync(DeadLetter deadLetter, CancellationToken cancellationToken = default)
        {
            Items.Add(deadLetter);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DeadLetter>> ListAsync(int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DeadLetter>>(Items);
    }

    private static ResilientNotificationEventPublisher Create(INotificationGateway gateway, IDeadLetterStore store)
        => new(
            gateway,
            store,
            new FixedClock(TestData.Now),
            new NotificationRetryOptions { MaxAttempts = 3, BaseDelayMilliseconds = 1 },
            NullLogger<ResilientNotificationEventPublisher>.Instance);

    private static IReadOnlyCollection<IDomainEvent> OneAssignedEvent()
        => new List<IDomainEvent> { new DeliveryAssigned(DeliveryId.New(), CourierId.New(), TestData.Now) };

    [Fact]
    public async Task Reintenta_y_tiene_exito_en_el_tercer_intento()
    {
        var gateway = new FlakyGateway(failures: 2);
        var store = new MemoryDeadLetterStore();

        await Create(gateway, store).PublishAsync(OneAssignedEvent());

        Assert.Equal(3, gateway.Calls);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Si_se_agotan_los_intentos_el_mensaje_va_a_la_cola_de_fallidos_sin_lanzar_excepcion()
    {
        var gateway = new FlakyGateway(failures: 99);
        var store = new MemoryDeadLetterStore();

        await Create(gateway, store).PublishAsync(OneAssignedEvent());

        Assert.Equal(3, gateway.Calls);
        var deadLetter = Assert.Single(store.Items);
        Assert.Equal(3, deadLetter.Attempts);
        Assert.Equal("NotificationUnavailableException", deadLetter.Error);
        Assert.Equal(TestData.Now, deadLetter.CreatedAt);
    }
}
