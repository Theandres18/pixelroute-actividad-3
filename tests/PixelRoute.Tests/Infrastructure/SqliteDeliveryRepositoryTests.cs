using PixelRoute.Application.Errors;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Persistence;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Infrastructure;

public sealed class SqliteDeliveryRepositoryTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private async Task<SqliteDeliveryRepository> CreateRepositoryAsync()
    {
        await _db.MigrateAsync();
        return new SqliteDeliveryRepository(_db.Factory);
    }

    [Fact]
    public async Task Guarda_y_lee_una_entrega_nueva()
    {
        var repository = await CreateRepositoryAsync();
        var delivery = TestData.PendingDelivery();

        await repository.SaveAsync(delivery);
        var loaded = await repository.GetAsync(delivery.Id);

        Assert.NotNull(loaded);
        Assert.Equal(DeliveryStatus.Pending, loaded!.Status);
        Assert.Equal(delivery.Address, loaded.Address);
        Assert.Equal(delivery.Promise, loaded.Promise);
        Assert.Equal(1, loaded.Version);
        Assert.Equal(1, delivery.Version);
    }

    [Fact]
    public async Task Persiste_la_asignacion_y_aumenta_la_version()
    {
        var repository = await CreateRepositoryAsync();
        var delivery = TestData.PendingDelivery();
        await repository.SaveAsync(delivery);

        var courier = TestData.AvailableCourier();
        var loaded = await repository.GetAsync(delivery.Id);
        loaded!.AssignCourier(courier, TestData.Now);
        await repository.SaveAsync(loaded);

        var reloaded = await repository.GetAsync(delivery.Id);
        Assert.Equal(DeliveryStatus.Assigned, reloaded!.Status);
        Assert.Equal(courier.CourierId, reloaded.AssignedCourierId);
        Assert.Equal(TestData.Now, reloaded.AssignedAt);
        Assert.Equal(2, reloaded.Version);
    }

    [Fact]
    public async Task Una_entrega_inexistente_devuelve_null()
    {
        var repository = await CreateRepositoryAsync();

        Assert.Null(await repository.GetAsync(DeliveryId.New()));
    }

    [Fact]
    public async Task Dos_asignaciones_concurrentes_de_la_misma_entrega_producen_un_conflicto()
    {
        var repository = await CreateRepositoryAsync();
        var original = TestData.PendingDelivery();
        await repository.SaveAsync(original);

        var copyA = await repository.GetAsync(original.Id);
        var copyB = await repository.GetAsync(original.Id);

        copyA!.AssignCourier(TestData.AvailableCourier(), TestData.Now);
        await repository.SaveAsync(copyA);

        copyB!.AssignCourier(TestData.AvailableCourier(), TestData.Now);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => repository.SaveAsync(copyB));

        var final = await repository.GetAsync(original.Id);
        Assert.Equal(copyA.AssignedCourierId, final!.AssignedCourierId);
    }

    [Fact]
    public async Task Un_texto_malicioso_se_guarda_como_dato_y_no_se_ejecuta()
    {
        var repository = await CreateRepositoryAsync();
        var malicious = "Calle 1'); DROP TABLE deliveries;--";
        var delivery = new Delivery(
            DeliveryId.New(),
            OrderId.New(),
            new DeliveryAddress(malicious),
            DeliveryPromise.FromCreation(TestData.Now));

        await repository.SaveAsync(delivery);
        var loaded = await repository.GetAsync(delivery.Id);

        Assert.Equal(malicious, loaded!.Address.Value);

        // La tabla sigue existiendo y funcionando.
        await repository.SaveAsync(TestData.PendingDelivery());
        Assert.Equal(2, await _db.CountAsync("deliveries"));
    }
}
