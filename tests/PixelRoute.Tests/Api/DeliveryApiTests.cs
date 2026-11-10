using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PixelRoute.Application.Abstractions;

namespace PixelRoute.Tests.Api;

/// <summary>
/// Pruebas de integración del flujo completo: HTTP, caso de uso, dominio y SQLite real.
/// Cada prueba usa su propia aplicación y su propia base de datos temporal.
/// </summary>
public class DeliveryApiTests
{
    private const string Address = "Calle 10 # 5-20, Barrio Centro";

    private static async Task<Guid> CreateDeliveryAsync(HttpClient client, string address = Address)
    {
        var response = await client.PostAsJsonAsync("/api/v1/deliveries", new { orderId = Guid.NewGuid(), address });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("deliveryId").GetGuid();
    }

    private static Task<HttpResponseMessage> AssignAsync(HttpClient client, Guid deliveryId, Guid courierId)
        => client.PostAsJsonAsync($"/api/v1/deliveries/{deliveryId}/assign-courier", new { courierId });

    private static async Task<string?> ErrorCodeAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("errorCode").GetString();
    }

    [Fact]
    public async Task Flujo_completo_crea_asigna_y_consulta_la_entrega()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var courierId = Guid.NewGuid();
        await factory.InsertCourierAsync(courierId);

        var deliveryId = await CreateDeliveryAsync(client);
        var assign = await AssignAsync(client, deliveryId, courierId);

        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);

        var get = await client.GetAsync($"/api/v1/deliveries/{deliveryId}");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var json = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Assigned", json.GetProperty("status").GetString());
        Assert.Equal(courierId, json.GetProperty("assignedCourierId").GetGuid());
        Assert.Equal(1, factory.Gateway.Calls);
    }

    [Fact]
    public async Task Limite_una_ruta_saturada_devuelve_422_y_no_asigna()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var courierId = Guid.NewGuid();
        await factory.InsertCourierAsync(courierId, maxActive: 1);

        var first = await CreateDeliveryAsync(client);
        var second = await CreateDeliveryAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await AssignAsync(client, first, courierId)).StatusCode);

        var rejected = await AssignAsync(client, second, courierId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rejected.StatusCode);
        Assert.Equal("courier_route_saturated", await ErrorCodeAsync(rejected));

        var json = await (await client.GetAsync($"/api/v1/deliveries/{second}")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Pending", json.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Asignar_una_entrega_que_ya_tiene_repartidor_devuelve_409()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var courierA = Guid.NewGuid();
        var courierB = Guid.NewGuid();
        await factory.InsertCourierAsync(courierA);
        await factory.InsertCourierAsync(courierB);
        var deliveryId = await CreateDeliveryAsync(client);

        Assert.Equal(HttpStatusCode.OK, (await AssignAsync(client, deliveryId, courierA)).StatusCode);

        var second = await AssignAsync(client, deliveryId, courierB);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("delivery_already_assigned", await ErrorCodeAsync(second));
    }

    [Fact]
    public async Task Un_repartidor_fuera_de_linea_devuelve_422()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var courierId = Guid.NewGuid();
        await factory.InsertCourierAsync(courierId, online: false);
        var deliveryId = await CreateDeliveryAsync(client);

        var response = await AssignAsync(client, deliveryId, courierId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("courier_not_available", await ErrorCodeAsync(response));
    }

    [Fact]
    public async Task Entrega_o_repartidor_inexistentes_devuelven_404()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var deliveryId = await CreateDeliveryAsync(client);

        var missingDelivery = await AssignAsync(client, Guid.NewGuid(), Guid.NewGuid());
        var missingCourier = await AssignAsync(client, deliveryId, Guid.NewGuid());

        Assert.Equal(HttpStatusCode.NotFound, missingDelivery.StatusCode);
        Assert.Equal("delivery_not_found", await ErrorCodeAsync(missingDelivery));
        Assert.Equal(HttpStatusCode.NotFound, missingCourier.StatusCode);
        Assert.Equal("courier_not_found", await ErrorCodeAsync(missingCourier));
    }

    [Fact]
    public async Task Sin_la_clave_de_despachador_se_rechaza_con_401_pero_la_salud_es_publica()
    {
        using var factory = new PixelRouteApiFactory();
        var anonymous = factory.CreateClient();

        var create = await anonymous.PostAsJsonAsync("/api/v1/deliveries", new { orderId = Guid.NewGuid(), address = Address });
        var health = await anonymous.GetAsync("/health");

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task Una_direccion_vacia_devuelve_400()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/v1/deliveries", new { orderId = Guid.NewGuid(), address = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Si_el_servicio_de_avisos_cae_la_asignacion_sigue_y_el_aviso_queda_en_la_cola_de_fallidos()
    {
        using var factory = new PixelRouteApiFactory();
        factory.Gateway.Fail = true;
        var client = factory.CreateAuthorizedClient();
        var courierId = Guid.NewGuid();
        await factory.InsertCourierAsync(courierId);
        var deliveryId = await CreateDeliveryAsync(client);

        var assign = await AssignAsync(client, deliveryId, courierId);

        Assert.Equal(HttpStatusCode.OK, assign.StatusCode);
        Assert.Equal(3, factory.Gateway.Calls);

        var deadLetters = await (await client.GetAsync("/api/v1/admin/dead-letters")).Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, deadLetters.GetArrayLength());
        Assert.Equal(3, deadLetters[0].GetProperty("attempts").GetInt32());
    }

    [Fact]
    public async Task Los_registros_no_contienen_la_direccion_ni_la_clave()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();
        var courierId = Guid.NewGuid();
        await factory.InsertCourierAsync(courierId);

        var deliveryId = await CreateDeliveryAsync(client, "Calle Secreta 123 Apto 45");
        await AssignAsync(client, deliveryId, courierId);

        var logs = factory.Logs.Messages.ToArray();
        Assert.NotEmpty(logs);
        Assert.DoesNotContain(logs, message => message.Contains("Secreta", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logs, message => message.Contains(PixelRouteApiFactory.ApiKey, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Un_error_inesperado_devuelve_500_generico_sin_detalles_internos()
    {
        using var factory = new PixelRouteApiFactory();
        using var failing = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDeliveryRepository>();
            services.AddScoped<IDeliveryRepository, ThrowingDeliveryRepository>();
        }));
        var client = failing.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", PixelRouteApiFactory.ApiKey);

        var response = await client.GetAsync($"/api/v1/deliveries/{Guid.NewGuid()}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain(ThrowingDeliveryRepository.SecretMessage, body);
        Assert.Contains("unexpected_error", body);
        Assert.True(response.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task El_identificador_de_correlacion_se_conserva_si_es_valido_y_se_reemplaza_si_no()
    {
        using var factory = new PixelRouteApiFactory();
        var client = factory.CreateAuthorizedClient();

        var valid = new HttpRequestMessage(HttpMethod.Get, "/health");
        valid.Headers.Add("X-Correlation-Id", "demo-001");
        var validResponse = await client.SendAsync(valid);

        var tooLong = new HttpRequestMessage(HttpMethod.Get, "/health");
        tooLong.Headers.Add("X-Correlation-Id", new string('a', 100));
        var tooLongResponse = await client.SendAsync(tooLong);

        Assert.Equal("demo-001", validResponse.Headers.GetValues("X-Correlation-Id").Single());
        Assert.NotEqual(new string('a', 100), tooLongResponse.Headers.GetValues("X-Correlation-Id").Single());
    }
}
