using Acorn.Dataverse.Cli.Connection;
using Acorn.Dataverse.Cli.Packages;
using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Cli.State;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Acorn.Dataverse.Cli.Commands;

/// <summary>Everything plan and deploy share: package, validation, connection, current state and plan.</summary>
internal sealed class DeploymentSession : IDisposable
{
    private DeploymentSession(
        FileInfo packageFile,
        PluginPackage package,
        ServiceClient client,
        SolutionInfo solution,
        DesiredState desired,
        DeploymentPlan plan)
    {
        PackageFile = packageFile;
        Package = package;
        Client = client;
        Solution = solution;
        Desired = desired;
        Plan = plan;
    }

    public FileInfo PackageFile { get; }

    public PluginPackage Package { get; }

    public ServiceClient Client { get; }

    public SolutionInfo Solution { get; }

    public DesiredState Desired { get; }

    public DeploymentPlan Plan { get; }

    public static async Task<DeploymentSession> OpenAsync(
        FileInfo packageFile,
        Uri environment,
        string? tenantId,
        string solutionName,
        bool prune,
        CancellationToken cancellationToken)
    {
        if (!packageFile.Exists)
        {
            throw new CliException(ExitCodes.UsageError, $"Package not found: {packageFile.FullName}");
        }

        var package = PluginPackage.Open(packageFile.FullName);
        ServiceClient? client = null;

        try
        {
            var plugins = MetadataPluginReader.Read(package.AssemblyPaths);
            var errors = plugins.SelectMany(plugin => PluginStepValidator.Validate(plugin)).ToList();
            if (errors.Count > 0)
            {
                throw new CliException(
                    ExitCodes.ValidationFailed,
                    "The package declarations are invalid:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Select(error => $"  - {error}")));
            }

            client = DataverseConnection.Connect(environment, tenantId);
            var reader = new EnvironmentReader(client);
            var solution = await reader.ReadSolutionAsync(solutionName, cancellationToken);

            var desired = new DesiredState($"{solution.PublisherPrefix}_{package.Id}", package.Version, plugins);
            var current = await reader.ReadAsync(desired.PackageUniqueName, cancellationToken);
            var plan = DeploymentPlanner.Create(desired, current, prune);

            return new DeploymentSession(packageFile, package, client, solution, desired, plan);
        }
        catch
        {
            client?.Dispose();
            package.Dispose();
            throw;
        }
    }

    public void PrintHeader()
    {
        var builtAt = File.GetLastWriteTime(PackageFile.FullName);
        Console.WriteLine($"Package     {Package.Id} {Package.Version} (built {builtAt:yyyy-MM-dd HH:mm:ss})");
        Console.WriteLine($"Environment {Client.ConnectedOrgFriendlyName} ({Client.ConnectedOrgUriActual?.Host})");
        Console.WriteLine($"Solution    {Solution.UniqueName} (prefix {Solution.PublisherPrefix})");
        Console.WriteLine();
    }

    public void Dispose()
    {
        Client.Dispose();
        Package.Dispose();
    }
}