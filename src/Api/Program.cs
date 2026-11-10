using PixelRoute.Api.Endpoints;
using PixelRoute.Api.Middleware;
using PixelRoute.Application.Abstractions;
using PixelRoute.Application.UseCases;
using PixelRoute.Infrastructure.Couriers;
using PixelRoute.Infrastructure.Notifications;
using PixelRoute.Infrastructure.Persistence;
using PixelRoute.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

// Raíz de composición: único lugar donde se eligen los adaptadores concretos para cada puerto.
// La configuración se lee de forma diferida para que las pruebas puedan reemplazarla.
builder.Services.AddSingleton(sp => new SqliteConnectionFactory(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("PixelRoute") ?? "Data Source=pixelroute.db"));
builder.Services.AddSingleton<SqlMigrationRunner>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IDeadLetterStore, SqliteDeadLetterStore>();
builder.Services.AddScoped<IDeliveryRepository, SqliteDeliveryRepository>();
builder.Services.AddScoped<ICourierAvailabilityProvider, SqliteCourierAvailabilityProvider>();

builder.Services.AddSingleton<INotificationGateway>(sp => new SimulatedNotificationGateway(
    sp.GetRequiredService<IConfiguration>()["Notifications:Mode"] ?? "Ok",
    sp.GetRequiredService<ILogger<SimulatedNotificationGateway>>()));
builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new NotificationRetryOptions
    {
        MaxAttempts = configuration.GetValue("Notifications:MaxAttempts", 3),
        BaseDelayMilliseconds = configuration.GetValue("Notifications:BaseDelayMilliseconds", 200)
    };
});
builder.Services.AddScoped<IDomainEventPublisher, ResilientNotificationEventPublisher>();

builder.Services.AddScoped<AssignCourierUseCase>();
builder.Services.AddScoped<CreateDeliveryUseCase>();
builder.Services.AddScoped<GetDeliveryUseCase>();
builder.Services.AddScoped<ListDeadLettersUseCase>();

builder.Services.AddHealthChecks();

var app = builder.Build();

// Migraciones reproducibles: se aplican al arrancar. Si fallan, la aplicación no inicia (falla de forma controlada).
var seedDemoData = app.Configuration.GetValue("Database:SeedDemoData", false);
await app.Services.GetRequiredService<SqlMigrationRunner>().RunAsync(seedDemoData);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapHealthChecks("/health");
app.MapDeliveryEndpoints();

app.Run();

// Permite que las pruebas de integración arranquen la aplicación completa.
public partial class Program { }
