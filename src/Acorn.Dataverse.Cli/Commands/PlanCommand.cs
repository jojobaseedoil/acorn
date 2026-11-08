using System.CommandLine;
using Acorn.Dataverse.Cli.Connection;
using Acorn.Dataverse.Cli.Packages;
using Acorn.Dataverse.Cli.Planning;
using Acorn.Dataverse.Cli.State;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Commands;

internal static class PlanCommand
{
    public static Command Create()
    {
        var packageArgument = DeploymentOptions.Package();
        var environmentOption = ConnectionOptions.Environment();
        var tenantOption = ConnectionOptions.Tenant();
        var solutionOption = DeploymentOptions.Solution();
        var pruneOption = DeploymentOptions.Prune();

        var command = new Command(
            "plan",
            "Compares the package declarations with the environment and shows what deploy would change. Changes nothing.")
        {
            packageArgument,
            environmentOption,
            tenantOption,
            solutionOption,
            pruneOption,
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult.GetValue(packageArgument)!,
            parseResult.GetValue(environmentOption)!,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(solutionOption)!,
            parseResult.GetValue(pruneOption),
            cancellationToken));

        return command;
    }

    private static async Task<int> RunAsync(
        FileInfo packageFile, Uri environment, string? tenantId, string solutionName, bool prune, CancellationToken cancellationToken)
    {
        if (!packageFile.Exists)
        {
            Console.Error.WriteLine($"Package not found: {packageFile.FullName}");
            return ExitCodes.UsageError;
        }

        using var package = PluginPackage.Open(packageFile.FullName);
        var plugins = MetadataPluginReader.Read(package.AssemblyPaths);

        var validationErrors = plugins.SelectMany(plugin => PluginStepValidator.Validate(plugin)).ToList();
        if (validationErrors.Count > 0)
        {
            Console.Error.WriteLine("The package declarations are invalid; run 'acorn validate' for details.");
            foreach (var error in validationErrors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return ExitCodes.ValidationFailed;
        }

        using var client = DataverseConnection.Connect(environment, tenantId);
        var reader = new EnvironmentReader(client);
        var solution = await reader.ReadSolutionAsync(solutionName, cancellationToken);

        var desired = new DesiredState($"{solution.PublisherPrefix}_{package.Id}", package.Version, plugins);
        var current = await reader.ReadAsync(desired.PackageUniqueName, cancellationToken);
        var plan = DeploymentPlanner.Create(desired, current, prune);

        var builtAt = File.GetLastWriteTime(packageFile.FullName);
        Console.WriteLine($"Package     {package.Id} {package.Version} (built {builtAt:yyyy-MM-dd HH:mm:ss})");
        Console.WriteLine($"Environment {client.ConnectedOrgFriendlyName} ({environment.Host})");
        Console.WriteLine($"Solution    {solution.UniqueName} (prefix {solution.PublisherPrefix})");
        Console.WriteLine();

        PlanPrinter.Print(plan);

        return plan.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }
}