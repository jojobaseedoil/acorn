using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Plugins.Tests.Registration;

public class PluginStepValidatorTests
{
    [Fact]
    public void Validate_ReturnsNoErrors_ForValidDeclarations()
        => Assert.Empty(PluginStepValidator.Validate(typeof(ValidPlugin)));

    [Theory]
    [InlineData(typeof(AsyncPreOperationPlugin), "asynchronous steps must be registered in PostOperation")]
    [InlineData(typeof(MainOperationPlugin), "MainOperation is reserved")]
    [InlineData(typeof(AutoDeleteOnSyncPlugin), "AsyncAutoDelete applies only")]
    [InlineData(typeof(FilteringOnCreatePlugin), "FilteringAttributes are supported only")]
    [InlineData(typeof(DuplicateStepPlugin), "declared more than once")]
    [InlineData(typeof(PreImageOnCreatePlugin), "pre-images are not available on Create")]
    [InlineData(typeof(PostImageOnPreOperationPlugin), "post-images are available only in PostOperation")]
    [InlineData(typeof(UnknownStepIdPlugin), "does not match any [PluginStep] Id")]
    [InlineData(typeof(ImageWithoutStepPlugin), "without any [PluginStep]")]
    public void Validate_ReportsError(Type pluginType, string expectedError)
    {
        var errors = PluginStepValidator.Validate(pluginType);

        Assert.Contains(errors, error => error.Contains(expectedError));
    }

    [Fact]
    public void Validate_SuggestsStepId_WhenSharedImageIsInvalidForOneStep()
    {
        var errors = PluginStepValidator.Validate(typeof(SharedPreImagePlugin));

        var error = Assert.Single(errors);
        Assert.Contains("Use StepId", error);
    }

    [Fact]
    public void Validate_AcceptsImageBoundToStepById()
        => Assert.Empty(PluginStepValidator.Validate(typeof(BoundPreImagePlugin)));

    [Fact]
    public void EnsureValid_Throws_WhenAssemblyHasInvalidDeclarations()
        => Assert.Throws<InvalidOperationException>(
            () => PluginStepValidator.EnsureValid(typeof(PluginStepValidatorTests).Assembly));

    private abstract class NoOpPlugin : PluginBase
    {
        protected override void ExecutePlugin(ILocalPluginContext context)
        {
        }
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account", Id = "update", FilteringAttributes = new[] { "creditlimit" })]
    [PluginStep(PipelineStage.PostOperation, MessageNames.Create, "account", Id = "create", Mode = ExecutionMode.Asynchronous, AsyncAutoDelete = true)]
    [PluginImage(ImageType.PreImage, "pre", StepId = "update", Attributes = new[] { "creditlimit" })]
    [PluginImage(ImageType.PostImage, "post", StepId = "create")]
    private sealed class ValidPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account", Mode = ExecutionMode.Asynchronous)]
    private sealed class AsyncPreOperationPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.MainOperation, "acorn_MyCustomApi")]
    private sealed class MainOperationPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PostOperation, MessageNames.Update, "account", AsyncAutoDelete = true)]
    private sealed class AutoDeleteOnSyncPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Create, "account", FilteringAttributes = new[] { "name" })]
    private sealed class FilteringOnCreatePlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account")]
    [PluginStep(PipelineStage.PreOperation, "update", "Account")]
    private sealed class DuplicateStepPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PostOperation, MessageNames.Create, "account")]
    [PluginImage(ImageType.PreImage, "pre")]
    private sealed class PreImageOnCreatePlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account")]
    [PluginImage(ImageType.PostImage, "post")]
    private sealed class PostImageOnPreOperationPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account")]
    [PluginImage(ImageType.PreImage, "pre", StepId = "missing")]
    private sealed class UnknownStepIdPlugin : NoOpPlugin
    {
    }

    [PluginImage(ImageType.PreImage, "pre")]
    private sealed class ImageWithoutStepPlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Create, "account")]
    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account")]
    [PluginImage(ImageType.PreImage, "pre")]
    private sealed class SharedPreImagePlugin : NoOpPlugin
    {
    }

    [PluginStep(PipelineStage.PreOperation, MessageNames.Create, "account")]
    [PluginStep(PipelineStage.PreOperation, MessageNames.Update, "account", Id = "update")]
    [PluginImage(ImageType.PreImage, "pre", StepId = "update")]
    private sealed class BoundPreImagePlugin : NoOpPlugin
    {
    }

        [Fact]
    public void Validate_WorksOnDefinitions_WithoutReflection()
    {
        var plugin = new PluginTypeDefinition
        {
            TypeName = "Contoso.Plugins.SamplePlugin",
            ImplementsPlugin = true,
            Steps = new[]
            {
                new PluginStepDefinition
                {
                    Stage = PipelineStage.PreOperation,
                    MessageName = MessageNames.Update,
                    PrimaryEntityName = "account",
                    Mode = ExecutionMode.Asynchronous,
                },
            },
        };

        var errors = PluginStepValidator.Validate(plugin);

        Assert.Contains(errors, error => error.Contains("asynchronous steps must be registered in PostOperation"));
    }

    [Fact]
    public void Reader_MapsAttributesToDefinitions()
    {
        var plugin = PluginRegistrationReader.Read(typeof(ValidPlugin));

        Assert.True(plugin.ImplementsPlugin);
        Assert.Equal(2, plugin.Steps.Count);
        Assert.Equal(2, plugin.Images.Count);

        var update = Assert.Single(plugin.Steps, s => s.Id == "update");
        Assert.Equal(new[] { "creditlimit" }, update.FilteringAttributes);
    }
}