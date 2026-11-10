using Microsoft.Data.Sqlite;

namespace PixelRoute.Tests.Infrastructure;

public sealed class SqlMigrationRunnerTests : IDisposable
{
    private readonly SqliteTestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Aplica_el_esquema_y_ejecutarlo_otra_vez_no_repite_nada()
    {
        var first = await _db.MigrateAsync();
        var second = await _db.MigrateAsync();

        Assert.Contains("Migrations.V001__create_schema.sql", first);
        Assert.Empty(second);
        Assert.Equal(0, await _db.CountAsync("deliveries"));
    }

    [Fact]
    public async Task Los_datos_de_demostracion_solo_se_cargan_si_se_pide()
    {
        await _db.MigrateAsync(includeSeed: false);
        Assert.Equal(0, await _db.CountAsync("couriers"));

        await _db.MigrateAsync(includeSeed: true);
        Assert.Equal(3, await _db.CountAsync("couriers"));
    }

    [Fact]
    public async Task El_esquema_rechaza_una_entrega_pendiente_con_repartidor()
    {
        await _db.MigrateAsync();

        await Assert.ThrowsAsync<SqliteException>(() => _db.ExecuteAsync(
            "INSERT INTO deliveries (id, order_id, address, promised_by, status, courier_id, assigned_at, version) " +
            "VALUES ('d1', 'o1', 'Calle 1', '2026-10-29T10:00:00+00:00', 'Pending', 'c1', NULL, 1)"));
    }

    [Fact]
    public async Task El_esquema_rechaza_una_direccion_vacia()
    {
        await _db.MigrateAsync();

        await Assert.ThrowsAsync<SqliteException>(() => _db.ExecuteAsync(
            "INSERT INTO deliveries (id, order_id, address, promised_by, status, courier_id, assigned_at, version) " +
            "VALUES ('d2', 'o2', '   ', '2026-10-29T10:00:00+00:00', 'Pending', NULL, NULL, 1)"));
    }
}
