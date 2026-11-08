using System.CommandLine;
using Acorn.Dataverse.Cli.Planning;

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
        using var session = await DeploymentSession.OpenAsync(
            packageFile, environment, tenantId, solutionName, prune, cancellationToken);

        session.PrintHeader();
        PlanPrinter.Print(session.Plan);

        return session.Plan.HasErrors ? ExitCodes.ValidationFailed : ExitCodes.Success;
    }
}