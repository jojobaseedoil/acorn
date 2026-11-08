using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Acorn.Dataverse.Cli.Deployment;

/// <summary>Turns a plan into Dataverse requests. Pure: builds requests, sends nothing.</summary>
internal static class DeploymentRequests
{
    internal const string StepEntityName = "sdkmessageprocessingstep";
    internal const string ImageEntityName = "sdkmessageprocessingstepimage";
    private const string SolutionParameter = "SolutionUniqueName";

    /// <summary>Phase A: steps and images to delete. Deleting a step also deletes its images.</summary>
    public static List<OrganizationRequest> Deletions(DeploymentPlan plan)
    {
        var requests = new List<OrganizationRequest>();

        foreach (var step in plan.Steps)
        {
            if (step.Kind == ChangeKind.Delete)
            {
                requests.Add(Delete(StepEntityName, step.Current!.Id));
                continue;
            }

            requests.AddRange(step.Images
                .Where(image => image.Kind == ChangeKind.Delete)
                .Select(image => Delete(ImageEntityName, image.Current!.Id)));
        }

        return requests;
    }

    /// <summary>Phase C: steps and images to create or update, all in the solution.</summary>
    public static List<OrganizationRequest> Writes(
        DeploymentPlan plan,
        ResolvedReferences references,
        IReadOnlyDictionary<string, Guid> pluginTypeIds,
        string solutionUniqueName)
    {
        var requests = new List<OrganizationRequest>();

        foreach (var step in plan.Steps)
        {
            Guid stepId;

            switch (step.Kind)
            {
                case ChangeKind.Create:
                {
                    var entity = StepForCreate(step, references, PluginTypeId(pluginTypeIds, step.TypeName));
                    stepId = entity.Id;
                    requests.Add(InSolution(new CreateRequest { Target = entity }, solutionUniqueName));
                    break;
                }

                case ChangeKind.Update:
                    stepId = step.Current!.Id;
                    requests.Add(InSolution(new UpdateRequest { Target = StepForUpdate(step, references) }, solutionUniqueName));
                    break;

                case ChangeKind.Unchanged:
                    stepId = step.Current!.Id;
                    break;

                default:
                    continue; // Deleted steps are handled in phase A.
            }

            var messageName = step.Desired!.MessageName;

            foreach (var image in step.Images)
            {
                if (image.Kind == ChangeKind.Create)
                {
                    var entity = ImageEntity(new Entity(ImageEntityName), image.Desired!, messageName, stepId);
                    requests.Add(InSolution(new CreateRequest { Target = entity }, solutionUniqueName));
                }
                else if (image.Kind == ChangeKind.Update)
                {
                    var entity = ImageEntity(new Entity(ImageEntityName, image.Current!.Id), image.Desired!, messageName, stepId: null);
                    requests.Add(InSolution(new UpdateRequest { Target = entity }, solutionUniqueName));
                }
            }
        }

        return requests;
    }

    internal static TRequest InSolution<TRequest>(TRequest request, string solutionUniqueName)
        where TRequest : OrganizationRequest
    {
        request.Parameters[SolutionParameter] = solutionUniqueName;
        return request;
    }

    private static Entity StepForCreate(StepChange change, ResolvedReferences references, Guid pluginTypeId)
    {
        var step = change.Desired!;

        // Id generated here, so images created in the same transaction can reference the new step.
        var entity = new Entity(StepEntityName, Guid.NewGuid())
        {
            ["eventhandler"] = new EntityReference("plugintype", pluginTypeId),
            ["sdkmessageid"] = new EntityReference("sdkmessage", references.MessageId(step.MessageName)),
            ["stage"] = new OptionSetValue((int)step.Stage),
            ["mode"] = new OptionSetValue((int)step.Mode),
            ["supporteddeployment"] = new OptionSetValue(0), // Server only
        };

        var filterId = references.FilterId(step);
        if (filterId is not null)
        {
            entity["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", filterId.Value);
        }

        ApplyUpdatableFields(entity, change.TypeName, step, references);
        return entity;
    }

    // Key fields (plug-in type, message, entities, stage, mode) never change on update: they define the step.
    private static Entity StepForUpdate(StepChange change, ResolvedReferences references)
    {
        var entity = new Entity(StepEntityName, change.Current!.Id);
        ApplyUpdatableFields(entity, change.TypeName, change.Desired!, references);
        return entity;
    }

    // Secure configuration is never written: it is defined in the environment, not in source code.
    private static void ApplyUpdatableFields(
        Entity entity, string typeName, PluginStepDefinition step, ResolvedReferences references)
    {
        entity["name"] = DeploymentPlanner.StepName(typeName, step);
        entity["description"] = DeploymentPlanner.StepDescription(typeName, step);
        entity["rank"] = step.ExecutionOrder;
        entity["filteringattributes"] = step.FilteringAttributes.Count > 0 ? string.Join(",", step.FilteringAttributes) : null;
        entity["configuration"] = step.UnsecureConfiguration;
        entity["asyncautodelete"] = step.AsyncAutoDelete;

        var runAsUserId = references.UserId(step.RunAsUser);
        entity["impersonatinguserid"] = runAsUserId is null ? null : new EntityReference("systemuser", runAsUserId.Value);
    }

    private static Entity ImageEntity(Entity entity, PluginImageDefinition image, string messageName, Guid? stepId)
    {
        if (stepId is not null)
        {
            entity["sdkmessageprocessingstepid"] = new EntityReference(StepEntityName, stepId.Value);
        }

        entity["imagetype"] = new OptionSetValue((int)image.ImageType);
        entity["entityalias"] = image.Alias;
        entity["name"] = image.Name ?? image.Alias;
        entity["attributes"] = image.Attributes is null ? null : string.Join(",", image.Attributes);
        entity["messagepropertyname"] = image.MessagePropertyName ?? MessagePropertyNames.For(messageName);
        return entity;
    }

    private static Guid PluginTypeId(IReadOnlyDictionary<string, Guid> pluginTypeIds, string typeName)
        => pluginTypeIds.TryGetValue(typeName, out var id)
            ? id
            : throw new InvalidOperationException(
                $"Plug-in type '{typeName}' was not found in the uploaded package. Is the class public and non-abstract?");

    private static DeleteRequest Delete(string entityName, Guid id)
        => new() { Target = new EntityReference(entityName, id) };
}