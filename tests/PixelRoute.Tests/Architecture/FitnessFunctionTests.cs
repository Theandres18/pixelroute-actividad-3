using PixelRoute.Application.UseCases;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Persistence;

namespace PixelRoute.Tests.Architecture;

/// <summary>
/// Funciones de aptitud (fitness functions): reglas de arquitectura que se verifican en cada ejecución de las pruebas
/// y también en la integración continua. Cada una traduce un riesgo del pre-mortem en una comprobación automática.
/// </summary>
public class FitnessFunctionTests
{
    private static IEnumerable<string> ReferencesOf(Type type)
        => type.Assembly.GetReferencedAssemblies().Select(assembly => assembly.Name ?? string.Empty);

    [Fact]
    public void El_dominio_no_depende_de_ningun_paquete_externo_de_Microsoft()
    {
        Assert.DoesNotContain(ReferencesOf(typeof(Delivery)), name => name.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void La_aplicacion_no_depende_de_ningun_paquete_externo_de_Microsoft()
    {
        Assert.DoesNotContain(ReferencesOf(typeof(AssignCourierUseCase)), name => name.StartsWith("Microsoft.", StringComparison.Ordinal));
    }

    [Fact]
    public void La_infraestructura_no_depende_de_ASP_NET_Core()
    {
        Assert.DoesNotContain(
            ReferencesOf(typeof(SqliteDeliveryRepository)),
            name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }

    [Fact]
    public void Los_casos_de_uso_solo_reciben_interfaces_por_constructor()
    {
        var useCases = typeof(AssignCourierUseCase).Assembly.GetTypes()
            .Where(type => type.Name.EndsWith("UseCase", StringComparison.Ordinal) && !type.IsAbstract)
            .ToList();

        Assert.NotEmpty(useCases);

        foreach (var useCase in useCases)
        {
            foreach (var constructor in useCase.GetConstructors())
            {
                foreach (var parameter in constructor.GetParameters())
                {
                    Assert.True(
                        parameter.ParameterType.IsInterface,
                        $"{useCase.Name} depende de la clase concreta {parameter.ParameterType.Name}.");
                }
            }
        }
    }

    [Fact]
    public void La_entidad_Delivery_no_expone_setters_publicos()
    {
        var publicSetters = typeof(Delivery).GetProperties()
            .Where(property => property.SetMethod is not null && property.SetMethod.IsPublic)
            .Select(property => property.Name)
            .ToList();

        Assert.Empty(publicSetters);
    }
}
