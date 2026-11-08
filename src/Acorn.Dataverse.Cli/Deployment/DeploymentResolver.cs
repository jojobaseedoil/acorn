using Acorn.Dataverse.Cli.Commands;
using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace Acorn.Dataverse.Cli.Deployment;

/// <summary>Ids the deployment needs, resolved before anything is written.</summary>
internal sealed class ResolvedReferences(
    Dictionary<string, Guid> messages,
    Dictionary<string, Guid?> filters,
    Dictionary<string, Guid> users)
{
    public Guid MessageId(string messageName) => messages[messageName];

    /// <summary>Message filter for the step, or <see langword="null"/> for messages registered for every entity.</summary>
    public Guid? FilterId(PluginStepDefinition step) => filters[FilterKey(step)];

    public Guid? UserId(string? domainName) => domainName is null ? null : users[domainName];

    internal static string FilterKey(PluginStepDefinition step)
        => $"{step.MessageName}|{step.PrimaryEntityName}|{step.SecondaryEntityName}";
}

/// <summary>
/// Preflight: resolves messages, message filters and run-as users for every step the plan writes.
/// Any problem is reported here, before the first change.
/// </summary>
internal static class DeploymentResolver
{
    public static async Task<ResolvedReferences> ResolveAsync(
        IOrganizationServiceAsync2 service, DeploymentPlan plan, CancellationToken cancellationToken)
    {
        var errors = new List<string>();

        var created = plan.Steps.Where(s => s.Kind == ChangeKind.Create).Select(s => s.Desired!).ToList();
        var written = plan.Steps.Where(s => s.Kind is ChangeKind.Create or ChangeKind.Update).Select(s => s.Desired!).ToList();

        var messages = await ResolveMessagesAsync(service, created.Select(s => s.MessageName), errors, cancellationToken);
        
        var entities = await ResolveEntitiesAsync(
            service,
            created.SelectMany(s => new[] { s.PrimaryEntityName, s.SecondaryEntityName }).OfType<string>(),
            errors,
            cancellationToken);

        var filters = new Dictionary<string, Guid?>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var step in created)
        {
            var key = ResolvedReferences.FilterKey(step);
            var entitiesExist = (step.PrimaryEntityName is null || entities.Contains(step.PrimaryEntityName))
                && (step.SecondaryEntityName is null || entities.Contains(step.SecondaryEntityName));

            if (entitiesExist && !filters.ContainsKey(key) && messages.TryGetValue(step.MessageName, out var messageId))
            {
                filters[key] = await ResolveFilterAsync(service, messageId, step, errors, cancellationToken);
            }
        }

        var users = await ResolveUsersAsync(
            service, written.Select(s => s.RunAsUser).OfType<string>(), errors, cancellationToken);

        if (errors.Count > 0)
        {
            throw new CliException(
                ExitCodes.ValidationFailed,
                "Preflight failed. Nothing was changed:" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => $"  - {error}")));
        }

        return new ResolvedReferences(messages, filters, users);
    }

    private static async Task<Dictionary<string, Guid>> ResolveMessagesAsync(
        IOrganizationServiceAsync2 service, IEnumerable<string> messageNames, List<string> errors, CancellationToken cancellationToken)
    {
        var names = messageNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var messages = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (names.Length == 0)
        {
            return messages;
        }

        var query = new QueryExpression("sdkmessage") { ColumnSet = new ColumnSet("name") };
        query.Criteria.AddCondition("name", ConditionOperator.In, names.Cast<object>().ToArray());

        foreach (var message in (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities)
        {
            messages[message.GetAttributeValue<string>("name")] = message.Id;
        }

        errors.AddRange(names
            .Where(name => !messages.ContainsKey(name))
            .Select(name => $"Message '{name}' does not exist in the environment."));

        return messages;
    }

    private static async Task<Guid?> ResolveFilterAsync(
        IOrganizationServiceAsync2 service,
        Guid messageId,
        PluginStepDefinition step,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var query = new QueryExpression("sdkmessagefilter") { ColumnSet = new ColumnSet(false), TopCount = 1 };
        query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageId);
        query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, step.PrimaryEntityName ?? "none");
        query.Criteria.AddCondition("secondaryobjecttypecode", ConditionOperator.Equal, step.SecondaryEntityName ?? "none");
        query.Criteria.AddCondition("iscustomprocessingstepallowed", ConditionOperator.Equal, true);

        var filter = (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities.FirstOrDefault();
        if (filter is not null)
        {
            return filter.Id;
        }

        // A step without entity is registered with no filter: it runs for every entity.
        if (step.PrimaryEntityName is null)
        {
            return null;
        }

        var secondary = step.SecondaryEntityName is null ? string.Empty : $" / '{step.SecondaryEntityName}'";
        errors.Add($"Message '{step.MessageName}' cannot be registered for entity '{step.PrimaryEntityName}'{secondary}.");
        return null;
    }

    private static async Task<Dictionary<string, Guid>> ResolveUsersAsync(
        IOrganizationServiceAsync2 service, IEnumerable<string> domainNames, List<string> errors, CancellationToken cancellationToken)
    {
        var names = domainNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var users = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        if (names.Length == 0)
        {
            return users;
        }

        var query = new QueryExpression("systemuser") { ColumnSet = new ColumnSet("domainname") };
        query.Criteria.AddCondition("domainname", ConditionOperator.In, names.Cast<object>().ToArray());

        foreach (var user in (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities)
        {
            users[user.GetAttributeValue<string>("domainname")] = user.Id;
        }

        errors.AddRange(names
            .Where(name => !users.ContainsKey(name))
            .Select(name => $"User '{name}' (RunAsUser) does not exist in the environment."));

        return users;
    }

    // Checked through metadata first: querying a message filter with an unknown entity name makes Dataverse
    // throw while translating the name, instead of returning no rows.
    private static async Task<HashSet<string>> ResolveEntitiesAsync(
        IOrganizationServiceAsync2 service, IEnumerable<string> entityNames, List<string> errors, CancellationToken cancellationToken)
    {
        var names = entityNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (names.Length == 0)
        {
            return existing;
        }

        var query = new EntityQueryExpression
        {
            Criteria = new MetadataFilterExpression(LogicalOperator.And),
            Properties = new MetadataPropertiesExpression("LogicalName"),
        };
        query.Criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.In, names));

        var response = (RetrieveMetadataChangesResponse)await service.ExecuteAsync(
            new RetrieveMetadataChangesRequest { Query = query }, cancellationToken);

        existing.UnionWith(response.EntityMetadata.Select(entity => entity.LogicalName));

        errors.AddRange(names
            .Where(name => !existing.Contains(name))
            .Select(name => $"Entity '{name}' does not exist in the environment."));

        return existing;
    }
}