using System.CommandLine;

namespace Acorn.Dataverse.Cli.Commands;

internal static class DeploymentOptions
{
    public static Argument<FileInfo> Package() => new("package")
    {
        Description = "Path to the plug-in package (.nupkg).",
    };

    public static Option<string> Solution() => new("--solution", "-s")
    {
        Description = "Unique name of the unmanaged solution that receives the components. Its publisher prefix names the package.",
        Required = true,
    };

    public static Option<bool> Prune() => new("--prune")
    {
        Description = "Deletes steps and images of this package that are no longer declared.",
    };
}