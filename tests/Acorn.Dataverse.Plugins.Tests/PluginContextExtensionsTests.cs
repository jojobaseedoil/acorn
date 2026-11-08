using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using NSubstitute;

namespace Acorn.Dataverse.Plugins.Tests;

public class PluginContextExtensionsTests
{
    private readonly ParameterCollection _inputParameters = new();
    private readonly ParameterCollection _outputParameters = new();
    private readonly EntityImageCollection _preImages = new();
    private readonly EntityImageCollection _postImages = new();
    private readonly IPluginExecutionContext _executionContext = Substitute.For<IPluginExecutionContext>();
    private readonly ILocalPluginContext _context = Substitute.For<ILocalPluginContext>();

    public PluginContextExtensionsTests()
    {
        _executionContext.InputParameters.Returns(_inputParameters);
        _executionContext.OutputParameters.Returns(_outputParameters);
        _executionContext.PreEntityImages.Returns(_preImages);
        _executionContext.PostEntityImages.Returns(_postImages);
        _executionContext.MessageName.Returns("Update");
        _context.ExecutionContext.Returns(_executionContext);
    }

    [Fact]
    public void GetTarget_ReturnsTypedTarget()
    {
        _inputParameters["Target"] = new Entity("account", Guid.NewGuid()) { ["name"] = "Contoso" };

        var account = _context.GetTarget<TestAccount>();

        Assert.Equal("Contoso", account.Name);
    }

    [Fact]
    public void GetTarget_SharesAttributesWithPipelineTarget()
    {
        var pipelineTarget = new Entity("account", Guid.NewGuid());
        _inputParameters["Target"] = pipelineTarget;

        var account = _context.GetTarget<TestAccount>();
        account.Name = "Changed in PreOperation";

        Assert.Equal("Changed in PreOperation", pipelineTarget.GetAttributeValue<string>("name"));
    }

    [Fact]
    public void GetTarget_Throws_WhenTargetIsEntityReference()
    {
        _inputParameters["Target"] = new EntityReference("account", Guid.NewGuid());

        Assert.Throws<InvalidPluginExecutionException>(() => _context.GetTarget<TestAccount>());
    }

    [Fact]
    public void GetTargetReference_ReturnsReference_WhenTargetIsEntityReference()
    {
        var reference = new EntityReference("account", Guid.NewGuid());
        _inputParameters["Target"] = reference;

        Assert.Same(reference, _context.GetTargetReference());
    }

    [Fact]
    public void GetTargetReference_UsesPrimaryEntityId_WhenTargetHasNoId()
    {
        var id = Guid.NewGuid();
        _executionContext.PrimaryEntityId.Returns(id);
        _inputParameters["Target"] = new Entity("account");

        var reference = _context.GetTargetReference();

        Assert.Equal("account", reference.LogicalName);
        Assert.Equal(id, reference.Id);
    }

    [Fact]
    public void GetPreImage_Throws_WithHint_WhenAliasIsMissing()
    {
        var ex = Assert.Throws<InvalidPluginExecutionException>(() => _context.GetPreImage<TestAccount>("pre"));

        Assert.Contains("[PluginImage]", ex.Message);
    }

    [Fact]
    public void GetMergedTarget_TargetWinsOverPreImage_IncludingClearedValues()
    {
        var id = Guid.NewGuid();
        _preImages["pre"] = new Entity("account", id) { ["name"] = "Old name", ["creditlimit"] = new Money(100m) };
        _inputParameters["Target"] = new Entity("account", id) { ["name"] = null };

        var merged = _context.GetMergedTarget<TestAccount>("pre");

        Assert.Null(merged.Name);
        Assert.Equal(100m, merged.GetAttributeValue<Money>("creditlimit").Value);
    }

    [Fact]
    public void GetMergedTarget_DoesNotChangePipelineTarget()
    {
        var pipelineTarget = new Entity("account", Guid.NewGuid());
        _preImages["pre"] = new Entity("account", pipelineTarget.Id);
        _inputParameters["Target"] = pipelineTarget;

        var merged = _context.GetMergedTarget<TestAccount>("pre");
        merged.Name = "Changed";

        Assert.False(pipelineTarget.Contains("name"));
    }

    [Fact]
    public void GetInputParameter_Throws_WhenTypeDiffers()
    {
        _inputParameters["Amount"] = "not a number";

        var ex = Assert.Throws<InvalidPluginExecutionException>(() => _context.GetInputParameter<decimal>("Amount"));

        Assert.Contains("expected Decimal", ex.Message);
    }

    [Fact]
    public void SetOutputParameter_WritesToOutputParameters()
    {
        _context.SetOutputParameter("Result", 42);

        Assert.Equal(42, _outputParameters["Result"]);
    }

    [EntityLogicalName("account")]
    private sealed class TestAccount : Entity
    {
        public TestAccount() : base("account")
        {
        }

        public string? Name
        {
            get => GetAttributeValue<string>("name");
            set => this["name"] = value;
        }
    }
}