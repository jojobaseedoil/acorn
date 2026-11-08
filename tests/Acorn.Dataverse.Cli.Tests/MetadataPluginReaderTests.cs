using System.Reflection;
using Acorn.Dataverse.Cli.Packages;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Tests;

public class MetadataPluginReaderTests
{
    private const string FixtureNamespace = "Acorn.Dataverse.Cli.Tests.Fixture";

    [Fact]
    public void Read_FindsOnlyConcretePluginTypes()
    {
        var plugins = ReadFixture();

        Assert.Equal(
            new[] { $"{FixtureNamespace}.FullyDeclaredPlugin", $"{FixtureNamespace}.MinimalPlugin" },
            plugins.Select(p => p.TypeName).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void Read_MapsEveryStepProperty()
    {
        var plugin = Single(ReadFixture(), "FullyDeclaredPlugin");

        var expected = new PluginStepDefinition
        {
            Stage = PipelineStage.PostOperation,
            MessageName = MessageNames.Update,
            PrimaryEntityName = "account",
            SecondaryEntityName = "contact",
            Mode = ExecutionMode.Asynchronous,
            ExecutionOrder = 5,
            FilteringAttributes = new[] { "name", "creditlimit" },
            Id = "update",
            Name = "Fully declared step",
            Description = "Every property is set",
            RunAsUser = "admin@contoso.com",
            AsyncAutoDelete = true,
            UnsecureConfiguration = "{\"key\":\"value\"}",
        };

        Assert.Equivalent(expected, Assert.Single(plugin.Steps), strict: true);
    }

    [Fact]
    public void Read_MapsEveryImageProperty()
    {
        var plugin = Single(ReadFixture(), "FullyDeclaredPlugin");

        var expected = new PluginImageDefinition
        {
            ImageType = ImageType.Both,
            Alias = "snapshot",
            Name = "Snapshot",
            Attributes = new[] { "name" },
            MessagePropertyName = "Target",
            StepId = "update",
        };

        Assert.Equivalent(expected, Assert.Single(plugin.Images), strict: true);
    }

    [Fact]
    public void Read_AppliesAttributeDefaults_WhenOptionalPropertiesAreNotSet()
    {
        var plugin = Single(ReadFixture(), "MinimalPlugin");

        var step = Assert.Single(plugin.Steps);
        Assert.Equal(ExecutionMode.Synchronous, step.Mode);
        Assert.Equal(1, step.ExecutionOrder);
        Assert.Empty(step.FilteringAttributes);
        Assert.False(step.AsyncAutoDelete);

        var image = Assert.Single(plugin.Images);
        Assert.Null(image.Attributes);
        Assert.Null(image.StepId);
    }

    private static IReadOnlyList<PluginTypeDefinition> ReadFixture()
    {
        var directory = Path.GetFullPath(typeof(MetadataPluginReaderTests).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "FixtureDirectory")
            .Value!);

        Assert.True(Directory.Exists(directory), $"Fixture not built at '{directory}'. Build the solution first.");

        return MetadataPluginReader.Read(Directory.GetFiles(directory, "*.dll"));
    }

    private static PluginTypeDefinition Single(IReadOnlyList<PluginTypeDefinition> plugins, string name)
        => Assert.Single(plugins, p => p.TypeName == $"{FixtureNamespace}.{name}");
}