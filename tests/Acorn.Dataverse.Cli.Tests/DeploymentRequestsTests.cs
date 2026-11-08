using Acorn.Dataverse.Cli.Deployment;
using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Acorn.Dataverse.Cli.Tests;

public class DeploymentRequestsTests
{
    private const string Solution = "AcornSamples";
    private const string PackageName = "cr39a_Contoso.Plugins";
    private const string TypeName = "Contoso.Plugins.ValidateCreditLimitPlugin";

    private static readonly Guid PluginTypeId = Guid.NewGuid();
    private static readonly Guid MessageId = Guid.NewGuid();
    private static readonly Guid FilterId = Guid.NewGuid();

    private static readonly PluginStepDefinition Step = new()
    {
        Stage = PipelineStage.PreOperation,
        MessageName = MessageNames.Update,
        PrimaryEntityName = "account",
        FilteringAttributes = ["creditlimit"],
    };

    private static readonly PluginImageDefinition Image = new()
    {
        ImageType = ImageType.PreImage,
        Alias = "pre",
        Attributes = ["creditlimit"],
    };

    [Fact]
    public void NewStep_IsCreatedWithItsKeyFields_InTheSolution()
    {
        var requests = Writes(Plan(new EnvironmentState()));

        var create = Assert.IsType<CreateRequest>(requests[0]);
        var step = create.Target;
        Assert.Equal(DeploymentRequests.StepEntityName, step.LogicalName);
        Assert.NotEqual(Guid.Empty, step.Id);
        Assert.Equal(PluginTypeId, step.GetAttributeValue<EntityReference>("eventhandler").Id);
        Assert.Equal(MessageId, step.GetAttributeValue<EntityReference>("sdkmessageid").Id);
        Assert.Equal(FilterId, step.GetAttributeValue<EntityReference>("sdkmessagefilterid").Id);
        Assert.Equal((int)PipelineStage.PreOperation, step.GetAttributeValue<OptionSetValue>("stage").Value);
        Assert.Equal("creditlimit", step.GetAttributeValue<string>("filteringattributes"));
        Assert.Equal(Solution, create.Parameters["SolutionUniqueName"]);
    }

    [Fact]
    public void ImagesOfNewStep_ReferenceThePreGeneratedStepId()
    {
        var requests = Writes(Plan(new EnvironmentState()));

        var stepId = Assert.IsType<CreateRequest>(requests[0]).Target.Id;
        var image = Assert.IsType<CreateRequest>(requests[1]).Target;
        Assert.Equal(DeploymentRequests.ImageEntityName, image.LogicalName);
        Assert.Equal(stepId, image.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id);
        Assert.Equal("Target", image.GetAttributeValue<string>("messagepropertyname"));
    }

    [Fact]
    public void ChangedStep_IsUpdated_WithoutTouchingKeyFields()
    {
        var current = CurrentStep() with { ExecutionOrder = 2 };

        var update = Assert.IsType<UpdateRequest>(Assert.Single(Writes(Plan(Existing(current)))));

        Assert.Equal(current.Id, update.Target.Id);
        Assert.Equal(1, update.Target.GetAttributeValue<int>("rank"));
        Assert.False(update.Target.Contains("eventhandler"));
        Assert.False(update.Target.Contains("sdkmessageid"));
        Assert.False(update.Target.Contains("stage"));
    }

    [Fact]
    public void UnchangedStep_ProducesNoRequests()
        => Assert.Empty(Writes(Plan(Existing(CurrentStep()))));

    [Fact]
    public void PrunedStep_IsDeletedInPhaseA_AndNotWrittenInPhaseC()
    {
        var undeclared = CurrentStep() with { Id = Guid.NewGuid(), MessageName = MessageNames.Delete, Images = [] };
        var plan = Plan(Existing(CurrentStep(), undeclared), prune: true);

        var delete = Assert.IsType<DeleteRequest>(Assert.Single(DeploymentRequests.Deletions(plan)));

        Assert.Equal(undeclared.Id, delete.Target.Id);
        Assert.Empty(Writes(plan));
    }

    [Fact]
    public void StepForAnyEntity_IsCreatedWithoutMessageFilter()
    {
        var requests = Writes(Plan(new EnvironmentState(), Step with { PrimaryEntityName = null }));

        Assert.False(Assert.IsType<CreateRequest>(requests[0]).Target.Contains("sdkmessagefilterid"));
    }

    private static List<OrganizationRequest> Writes(DeploymentPlan plan)
    {
        var filters = plan.Steps
            .Where(step => step.Desired is not null)
            .ToDictionary(
                step => ResolvedReferences.FilterKey(step.Desired!),
                step => step.Desired!.PrimaryEntityName is null ? (Guid?)null : FilterId,
                StringComparer.OrdinalIgnoreCase);

        var references = new ResolvedReferences(
            new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase) { [MessageNames.Update] = MessageId },
            filters,
            new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase));

        return DeploymentRequests.Writes(
            plan, references, new Dictionary<string, Guid> { [TypeName] = PluginTypeId }, Solution);
    }

    private static DeploymentPlan Plan(EnvironmentState current, PluginStepDefinition? step = null, bool prune = false)
    {
        var desired = new DesiredState(PackageName, "1.0.0",
        [
            new PluginTypeDefinition
            {
                TypeName = TypeName,
                ImplementsPlugin = true,
                Steps = [step ?? Step],
                Images = [Image],
            },
        ]);

        return DeploymentPlanner.Create(desired, current, prune);
    }

    private static EnvironmentState Existing(params StepState[] steps) => new()
    {
        Package = new PackageState(Guid.NewGuid(), PackageName, "1.0.0"),
        Steps = steps,
    };

    private static StepState CurrentStep() => new()
    {
        Id = Guid.NewGuid(),
        TypeName = TypeName,
        MessageName = MessageNames.Update,
        PrimaryEntityName = "account",
        Stage = PipelineStage.PreOperation,
        Mode = ExecutionMode.Synchronous,
        ExecutionOrder = 1,
        FilteringAttributes = ["creditlimit"],
        Name = $"{TypeName}: Update of account",
        Description = $"{TypeName}: Update of account",
        Images =
        [
            new ImageState
            {
                Id = Guid.NewGuid(),
                ImageType = ImageType.PreImage,
                Alias = "pre",
                Name = "pre",
                Attributes = ["creditlimit"],
                MessagePropertyName = "Target",
            },
        ],
    };
}