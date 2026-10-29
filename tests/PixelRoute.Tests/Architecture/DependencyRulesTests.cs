using PixelRoute.Application.UseCases;
using PixelRoute.Domain.Deliveries;
using PixelRoute.Infrastructure.Persistence;

namespace PixelRoute.Tests.Architecture;

/// <summary>Pruebas de arquitectura: comprueban que las dependencias apuntan hacia adentro (CV-01).</summary>
public class DependencyRulesTests
{
    private static List<string?> ReferencedBy(Type type)
        => type.Assembly.GetReferencedAssemblies().Select(a => a.Name).ToList();

    [Fact]
    public void Domain_no_depende_de_Application_ni_de_Infrastructure()
    {
        var references = ReferencedBy(typeof(Delivery));

        Assert.DoesNotContain("PixelRoute.Application", references);
        Assert.DoesNotContain("PixelRoute.Infrastructure", references);
    }

    [Fact]
    public void Application_depende_de_Domain_pero_no_de_Infrastructure()
    {
        var references = ReferencedBy(typeof(AssignCourierUseCase));

        Assert.Contains("PixelRoute.Domain", references);
        Assert.DoesNotContain("PixelRoute.Infrastructure", references);
    }

    [Fact]
    public void Infrastructure_depende_de_Application()
    {
        var references = ReferencedBy(typeof(InMemoryDeliveryRepository));

        Assert.Contains("PixelRoute.Application", references);
    }
}
