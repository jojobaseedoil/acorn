using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Acorn.Dataverse.Cli.State;

internal sealed record SolutionInfo(Guid Id, string UniqueName, string PublisherPrefix);

/// <summary>Reads what exists in the environment for a plug-in package: package, steps and images.</summary>
internal sealed class EnvironmentReader(IOrganizationServiceAsync2 service)
{
    public async Task<SolutionInfo> ReadSolutionAsync(string uniqueName, CancellationToken cancellationToken)
    {
        var query = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("ismanaged"),
            TopCount = 1,
        };
        query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

        var publisher = query.AddLink("publisher", "publisherid", "publisherid");
        publisher.EntityAlias = "publisher";
        publisher.Columns = new ColumnSet("customizationprefix");

        var solution = (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"Solution '{uniqueName}' was not found. Use the solution unique name, not its display name.");

        if (solution.GetAttributeValue<bool>("ismanaged"))
        {
            throw new InvalidOperationException(
                $"Solution '{uniqueName}' is managed. Components can only be added to an unmanaged solution.");
        }

        var prefix = Aliased<string>(solution, "publisher.customizationprefix")
            ?? throw new InvalidOperationException($"The publisher of solution '{uniqueName}' has no customization prefix.");

        return new SolutionInfo(solution.Id, uniqueName, prefix);
    }

    public async Task<EnvironmentState> ReadAsync(string packageUniqueName, CancellationToken cancellationToken)
    {
        var package = await ReadPackageAsync(packageUniqueName, cancellationToken);
        if (package is null)
        {
            return new EnvironmentState();
        }

        var assemblyIds = await ReadAssemblyIdsAsync(package.Id, cancellationToken);
        var steps = await ReadStepsAsync(assemblyIds, cancellationToken);

        return new EnvironmentState { Package = package, Steps = steps };
    }

    private async Task<PackageState?> ReadPackageAsync(string uniqueName, CancellationToken cancellationToken)
    {
        var query = new QueryExpression("pluginpackage")
        {
            ColumnSet = new ColumnSet("uniquename", "version"),
            TopCount = 1,
        };
        query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, uniqueName);

        var package = (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.FirstOrDefault();

        return package is null
            ? null
            : new PackageState(package.Id, package.GetAttributeValue<string>("uniquename"), package.GetAttributeValue<string>("version"));
    }

    private async Task<List<Guid>> ReadAssemblyIdsAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var query = new QueryExpression("pluginassembly") { ColumnSet = new ColumnSet(false) };
        query.Criteria.AddCondition("packageid", ConditionOperator.Equal, packageId);

        return (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.Select(e => e.Id).ToList();
    }

    private async Task<List<StepState>> ReadStepsAsync(List<Guid> assemblyIds, CancellationToken cancellationToken)
    {
        if (assemblyIds.Count == 0)
        {
            return [];
        }

        var query = new QueryExpression("sdkmessageprocessingstep")
        {
            ColumnSet = new ColumnSet(
                "name", "stage", "mode", "rank", "filteringattributes", "configuration", "description", "asyncautodelete"),
        };

        var type = query.AddLink("plugintype", "eventhandler", "plugintypeid");
        type.EntityAlias = "type";
        type.Columns = new ColumnSet("typename");
        type.LinkCriteria.AddCondition("pluginassemblyid", ConditionOperator.In, assemblyIds.Cast<object>().ToArray());

        var message = query.AddLink("sdkmessage", "sdkmessageid", "sdkmessageid");
        message.EntityAlias = "message";
        message.Columns = new ColumnSet("name");

        var filter = query.AddLink("sdkmessagefilter", "sdkmessagefilterid", "sdkmessagefilterid", JoinOperator.LeftOuter);
        filter.EntityAlias = "filter";
        filter.Columns = new ColumnSet("primaryobjecttypecode", "secondaryobjecttypecode");

        var runAs = query.AddLink("systemuser", "impersonatinguserid", "systemuserid", JoinOperator.LeftOuter);
        runAs.EntityAlias = "runas";
        runAs.Columns = new ColumnSet("domainname");

        var steps = (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.ToList();
        var images = await ReadImagesAsync(steps.Select(s => s.Id).ToList(), cancellationToken);

        return steps.Select(step => ToStep(step, images)).ToList();
    }

    private async Task<ILookup<Guid, ImageState>> ReadImagesAsync(List<Guid> stepIds, CancellationToken cancellationToken)
    {
        List<Entity> images = [];

        if (stepIds.Count > 0)
        {
            var query = new QueryExpression("sdkmessageprocessingstepimage")
            {
                ColumnSet = new ColumnSet(
                    "sdkmessageprocessingstepid", "imagetype", "name", "entityalias", "attributes", "messagepropertyname"),
            };
            query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, stepIds.Cast<object>().ToArray());

            images = (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.ToList();
        }

        return images.ToLookup(
            image => image.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid").Id,
            ToImage);
    }

    private static StepState ToStep(Entity step, ILookup<Guid, ImageState> images) => new()
    {
        Id = step.Id,
        TypeName = Aliased<string>(step, "type.typename") ?? string.Empty,
        MessageName = Aliased<string>(step, "message.name") ?? string.Empty,
        PrimaryEntityName = Aliased<string>(step, "filter.primaryobjecttypecode"),
        SecondaryEntityName = Aliased<string>(step, "filter.secondaryobjecttypecode"),
        Stage = (PipelineStage)step.GetAttributeValue<OptionSetValue>("stage").Value,
        Mode = (ExecutionMode)step.GetAttributeValue<OptionSetValue>("mode").Value,
        ExecutionOrder = step.GetAttributeValue<int>("rank"),
        FilteringAttributes = SplitAttributes(step.GetAttributeValue<string>("filteringattributes")) ?? [],
        Name = step.GetAttributeValue<string>("name") ?? string.Empty,
        Description = step.GetAttributeValue<string>("description"),
        UnsecureConfiguration = step.GetAttributeValue<string>("configuration"),
        RunAsUser = Aliased<string>(step, "runas.domainname"),
        AsyncAutoDelete = step.GetAttributeValue<bool>("asyncautodelete"),
        Images = images[step.Id].ToList(),
    };

    private static ImageState ToImage(Entity image) => new()
    {
        Id = image.Id,
        ImageType = (ImageType)image.GetAttributeValue<OptionSetValue>("imagetype").Value,
        Alias = image.GetAttributeValue<string>("entityalias") ?? string.Empty,
        Name = image.GetAttributeValue<string>("name") ?? string.Empty,
        Attributes = SplitAttributes(image.GetAttributeValue<string>("attributes")),
        MessagePropertyName = image.GetAttributeValue<string>("messagepropertyname") ?? string.Empty,
    };

    private static string[]? SplitAttributes(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static T? Aliased<T>(Entity entity, string name) where T : class
        => entity.GetAttributeValue<AliasedValue>(name)?.Value as T;
}