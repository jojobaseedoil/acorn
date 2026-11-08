using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Tests;

public class DeploymentPlannerTests
{
    private const string PackageName = "cr39a_Contoso.Plugins";
    private const string TypeName = "Contoso.Plugins.ValidateCreditLimitPlugin";

    private static readonly PluginStepDefinition UpdateStep = new()
    {
        Stage = PipelineStage.PreOperation,
        MessageName = MessageNames.Update,
        PrimaryEntityName = "account",
        FilteringAttributes = ["creditlimit"],
    };

    private static readonly PluginImageDefinition PreImage = new()
    {
        ImageType = ImageType.PreImage,
        Alias = "pre",
        Attributes = ["creditlimit"],
    };

    [Fact]
    public void NewPackage_CreatesPackageStepsAndImages()
    {
        var plan = Plan(new EnvironmentState());

        Assert.Equal(ChangeKind.Create, plan.Package.Kind);
        var step = Assert.Single(plan.Steps);
        Assert.Equal(ChangeKind.Create, step.Kind);
        Assert.Equal(ChangeKind.Create, Assert.Single(step.Images).Kind);
    }

    [Fact]
    public void ExistingStep_MatchingTheDeclaration_IsAdoptedUnchanged()
    {
        var plan = Plan(Existing(CurrentStep()));

        var step = Assert.Single(plan.Steps);
        Assert.Equal(ChangeKind.Unchanged, step.Kind);
        Assert.Equal(ChangeKind.Unchanged, Assert.Single(step.Images).Kind);
    }

    [Fact]
    public void FilteringAttributes_AreComparedAsSets()
    {
        var declared = UpdateStep with { FilteringAttributes = ["creditlimit", "name"] };
        var current = CurrentStep() with { FilteringAttributes = ["Name", "creditlimit", "name"] };

        var plan = Plan(Existing(current), declared);

        Assert.Equal(ChangeKind.Unchanged, Assert.Single(plan.Steps).Kind);
    }

    [Fact]
    public void ChangedExecutionOrder_IsAnUpdate_ListingTheDifference()
    {
        var plan = Plan(Existing(CurrentStep() with { ExecutionOrder = 2 }));

        var step = Assert.Single(plan.Steps);
        Assert.Equal(ChangeKind.Update, step.Kind);
        Assert.Contains(step.Differences, difference => difference.StartsWith("execution order", StringComparison.Ordinal));
    }

    [Fact]
    public void UndeclaredStep_IsOrphan_WithoutPrune()
    {
        var undeclared = CurrentStep() with { MessageName = MessageNames.Delete, Images = [] };

        var plan = Plan(Existing(CurrentStep(), undeclared));

        Assert.Contains(plan.Steps, step => step.Kind == ChangeKind.Orphan && step.Current == undeclared);
    }

    [Fact]
    public void UndeclaredStep_IsDeleted_WithPrune()
    {
        var undeclared = CurrentStep() with { MessageName = MessageNames.Delete, Images = [] };

        var plan = Plan(Existing(CurrentStep(), undeclared), prune: true);

        Assert.Contains(plan.Steps, step => step.Kind == ChangeKind.Delete && step.Current == undeclared);
    }

    [Fact]
    public void ChangedStage_CreatesNewStep_AndOrphansTheOldOne()
    {
        var declared = UpdateStep with { Stage = PipelineStage.PostOperation };

        var plan = Plan(Existing(CurrentStep()), declared);

        Assert.Contains(plan.Steps, step => step.Kind == ChangeKind.Create);
        Assert.Contains(plan.Steps, step => step.Kind == ChangeKind.Orphan);
    }

    [Fact]
    public void StepForAnyEntity_MatchesDataverseNone()
    {
        var declared = UpdateStep with { PrimaryEntityName = null };
        var current = CurrentStep() with
        {
            PrimaryEntityName = "none",
            Name = $"{TypeName}: Update of any Entity",
            Description = $"{TypeName}: Update of any Entity",
        };

        var plan = Plan(Existing(current), declared);

        Assert.Equal(ChangeKind.Unchanged, Assert.Single(plan.Steps).Kind);
    }

    [Fact]
    public void ChangedPackageVersion_IsAnError()
    {
        var current = Existing(CurrentStep()) with
        {
            Package = new PackageState(Guid.NewGuid(), PackageName, "2.0.0"),
        };

        var plan = Plan(current);

        Assert.True(plan.HasErrors);
        Assert.Contains("version", Assert.Single(plan.Errors), StringComparison.Ordinal);
    }

    [Fact]
    public void DeclaredDescription_OverridesTheDefault()
    {
        var declared = UpdateStep with { Description = "Blocks credit limit increases above 50%." };

        var plan = Plan(Existing(CurrentStep()), declared);

        var step = Assert.Single(plan.Steps);
        Assert.Equal(ChangeKind.Update, step.Kind);
        Assert.Contains(step.Differences, difference => difference.StartsWith("description", StringComparison.Ordinal));
    }

    private static DeploymentPlan Plan(EnvironmentState current, PluginStepDefinition? step = null, bool prune = false)
    {
        var desired = new DesiredState(PackageName, "1.0.0",
        [
            new PluginTypeDefinition
            {
                TypeName = TypeName,
                ImplementsPlugin = true,
                Steps = [step ?? UpdateStep],
                Images = [PreImage],
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
        Description = $"{TypeName}: Update of account",
    };
}