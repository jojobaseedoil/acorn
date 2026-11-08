using System.CommandLine;
using Acorn.Dataverse.Cli.Deployment;

namespace Acorn.Dataverse.Cli.Commands;

internal static class DeployCommand
{
    public static Command Create()
    {
        var packageArgument = DeploymentOptions.Package();
        var environmentOption = ConnectionOptions.Environment();
        var tenantOption = ConnectionOptions.Tenant();
        var solutionOption = DeploymentOptions.Solution();
        var pruneOption = DeploymentOptions.Prune();
        var yesOption = new Option<bool>("--yes", "-y")
        {
            Description = "Applies without asking for confirmation. Required when not running interactively (e.g. in CI).",
        };

        var command = new Command(
            "deploy",
            "Uploads the plug-in package and creates, updates or deletes its steps and images to match the declarations.")
        {
            packageArgument,
            environmentOption,
            tenantOption,
            solutionOption,
            pruneOption,
            yesOption,
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult.GetValue(packageArgument)!,
            parseResult.GetValue(environmentOption)!,
            parseResult.GetValue(tenantOption),
            parseResult.GetValue(solutionOption)!,
            parseResult.GetValue(pruneOption),
            parseResult.GetValue(yesOption),
            cancellationToken));

        return command;
    }

    private static async Task<int> RunAsync(
        FileInfo packageFile,
        Uri environment,
        string? tenantId,
        string solutionName,
        bool prune,
        bool yes,
        CancellationToken cancellationToken)
    {
        using var session = await DeploymentSession.OpenAsync(
            packageFile, environment, tenantId, solutionName, prune, cancellationToken);

        session.PrintHeader();
        PlanPrinter.Print(session.Plan);
        Console.WriteLine();

        if (session.Plan.HasErrors)
        {
            return ExitCodes.ValidationFailed;
        }

        if (session.Plan.HasOrphans)
        {
            throw new CliException(
                ExitCodes.ValidationFailed,
                "Some steps or images of this package are no longer declared. Undeclared steps that stay registered " +
                "fail at runtime, because PluginBase checks every execution against the declarations. " +
                "Run again with --prune to delete them.");
        }

        _ = await DeploymentResolver.ResolveAsync(session.Client, session.Plan, cancellationToken);
        Console.WriteLine("Preflight OK.");

        if (!yes && !Confirm())
        {
            Console.WriteLine("Cancelled. Nothing was changed.");
            return ExitCodes.Success;
        }

        // 8.5b: DeploymentExecutor applies phases A (delete), B (package) and C (steps and images).
        Console.WriteLine("(execution not implemented yet: nothing was changed)");
        return ExitCodes.Success;
    }

    private static bool Confirm()
    {
        if (Console.IsInputRedirected)
        {
            throw new CliException(
                ExitCodes.UsageError, "Not running interactively: pass --yes to apply the changes.");
        }

        Console.Write("Apply these changes? (y/N) ");
        var answer = Console.ReadLine()?.Trim();

        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
            || string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase);
    }
}