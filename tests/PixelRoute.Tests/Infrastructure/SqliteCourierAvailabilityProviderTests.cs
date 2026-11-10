using PixelRoute.Domain.Couriers;
using PixelRoute.Infrastructure.Couriers;
using PixelRoute.Infrastructure.Persistence;
using PixelRoute.Tests.Support;

namespace PixelRoute.Tests.Infrastructure;

public sealed class SqliteCourierAvailabilityProviderTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Cuenta_las_entregas_activas_y_detecta_la_ruta_saturada()
    {
        await _db.MigrateAsync();
        var courierId = Guid.NewGuid();
        await _db.InsertCourierAsync(courierId, online: true, maxActive: 2);

        var repository = new SqliteDeliveryRepository(_db.Factory);
        for (var i = 0; i < 2; i++)
        {
            var delivery = TestData.PendingDelivery();
            delivery.AssignCourier(new CourierAvailability(new CourierId(courierId), true, 0, 2), TestData.Now);
            await repository.SaveAsync(delivery);
        }

        var provider = new SqliteCourierAvailabilityProvider(_db.Factory);
        var availability = await provider.GetAvailabilityAsync(new CourierId(courierId));

        Assert.NotNull(availability);
        Assert.True(availability!.IsOnline);
        Assert.Equal(2, availability.ActiveDeliveries);
        Assert.Equal(2, availability.MaxActiveDeliveries);
        Assert.True(availability.IsRouteSaturated);
    }

    [Fact]
    public async Task Un_repartidor_inexistente_devuelve_null_y_uno_fuera_de_linea_se_reporta_asi()
    {
        await _db.MigrateAsync();
        var offlineId = Guid.NewGuid();
        await _db.InsertCourierAsync(offlineId, online: false);
        var provider = new SqliteCourierAvailabilityProvider(_db.Factory);

        Assert.Null(await provider.GetAvailabilityAsync(CourierId.New()));

        var offline = await provider.GetAvailabilityAsync(new CourierId(offlineId));
        Assert.False(offline!.IsOnline);
    }
}
