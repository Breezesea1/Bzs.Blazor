using System.Reflection;

namespace Bzs.Blazor.Tests;

public sealed class PublicApiContractTests
{
    [Fact]
    public void Concrete_public_components_are_sealed()
    {
        var components = typeof(BzsComponentBase).Assembly
            .GetExportedTypes()
            .Where(type => !type.IsAbstract && typeof(BzsComponentBase).IsAssignableFrom(type))
            .ToArray();

        Assert.NotEmpty(components);
        Assert.DoesNotContain(components, type => !type.IsSealed);
    }

    [Theory]
    [InlineData(typeof(BzsComponentBase))]
    [InlineData(typeof(BzsInputBase<>))]
    public void Public_component_bases_have_no_externally_accessible_constructor(Type baseType)
    {
        var constructors = baseType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        Assert.NotEmpty(constructors);
        Assert.DoesNotContain(
            constructors,
            constructor => constructor.IsPublic || constructor.IsFamily || constructor.IsFamilyOrAssembly);
    }
}
