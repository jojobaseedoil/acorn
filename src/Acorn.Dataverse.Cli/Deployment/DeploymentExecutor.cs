using Acorn.Dataverse.Cli.Commands;
using Acorn.Dataverse.Cli.Planning;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Acorn.Dataverse.Cli.Deployment;

/// <summary>Applies a plan: A) deletions, B) package upload, C) step and image writes.</summary>
internal sealed class DeploymentExecutor(IOrganizationServiceAsync2 service)
{
    private const int TransactionLimit = 1000;

    public async Task ExecuteAsync(DeploymentSession session, ResolvedReferences references, CancellationToken cancellationToken)
    {
        var plan = session.Plan;

        await ExecuteInTransactionAsync(
            "deleting undeclared steps and images", DeploymentRequests.Deletions(plan), cancellationToken);

        Console.WriteLine($"uploading package {plan.Package.UniqueName}...");
        var packageId = await UploadPackageAsync(session, cancellationToken);

        var pluginTypeIds = await ReadPluginTypeIdsAsync(packageId, cancellationToken);
        await ExecuteInTransactionAsync(
            "creating and updating steps and images",
            DeploymentRequests.Writes(plan, references, pluginTypeIds, session.Solution.UniqueName),
            cancellationToken);
    }

    private async Task<Guid> UploadPackageAsync(DeploymentSession session, CancellationToken cancellationToken)
    {
        var package = session.Plan.Package;
        var content = Convert.ToBase64String(await File.ReadAllBytesAsync(session.PackageFile.FullName, cancellationToken));

        if (package.Kind == ChangeKind.Create)
        {
            var entity = new Entity("pluginpackage")
            {
                ["name"] = package.UniqueName,
                ["uniquename"] = package.UniqueName,
                ["version"] = package.Version,
                ["content"] = content,
            };

            var response = (CreateResponse)await service.ExecuteAsync(
                DeploymentRequests.InSolution(new CreateRequest { Target = entity }, session.Solution.UniqueName),
                cancellationToken);

            return response.id;
        }

        var packageId = package.Id ?? throw new InvalidOperationException("The plan has no id for the existing package.");
        var update = new Entity("pluginpackage", packageId) { ["content"] = content };

        await service.ExecuteAsync(
            DeploymentRequests.InSolution(new UpdateRequest { Target = update }, session.Solution.UniqueName),
            cancellationToken);

        return packageId;
    }

    // After the upload, Dataverse has created or updated the plug-in types of the package.
    private async Task<Dictionary<string, Guid>> ReadPluginTypeIdsAsync(Guid packageId, CancellationToken cancellationToken)
    {
        var query = new QueryExpression("plugintype") { ColumnSet = new ColumnSet("typename") };
        var assembly = query.AddLink("pluginassembly", "pluginassemblyid", "pluginassemblyid");
        assembly.LinkCriteria.AddCondition("packageid", ConditionOperator.Equal, packageId);

        return (await service.RetrieveMultipleAsync(query, cancellationToken)).Entities
            .GroupBy(type => type.GetAttributeValue<string>("typename"), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
    }

    private async Task ExecuteInTransactionAsync(
        string description, List<OrganizationRequest> requests, CancellationToken cancellationToken)
    {
        if (requests.Count == 0)
        {
            return;
        }

        if (requests.Count > TransactionLimit)
        {
            throw new CliException(
                ExitCodes.Failure,
                $"{description}: {requests.Count} operations exceed the Dataverse transaction limit of {TransactionLimit}.");
        }

        Console.WriteLine($"{description} ({requests.Count} operation(s))...");

        var transaction = new ExecuteTransactionRequest
        {
            Requests = new OrganizationRequestCollection(),
            ReturnResponses = false,
        };
        transaction.Requests.AddRange(requests);

        await service.ExecuteAsync(transaction, cancellationToken);
    }
}