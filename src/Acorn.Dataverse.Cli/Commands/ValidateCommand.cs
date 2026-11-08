using System.CommandLine;
using Acorn.Dataverse.Cli.Packages;
using Acorn.Dataverse.Plugins.Registration;

namespace Acorn.Dataverse.Cli.Commands;

internal static class ValidateCommand
{
    public static Command Create()
    {
        var packageArgument = DeploymentOptions.Package();
        
        var command = new Command(
            "validate",
            "Reads the [PluginStep] and [PluginImage] declarations of a plug-in package and validates them. Does not connect to Dataverse.")
        {
            packageArgument,
        };

        command.SetAction(parseResult => Run(parseResult.GetValue(packageArgument)!));
        return command;
    }

    private static int Run(FileInfo packageFile)
    {
        if (!packageFile.Exists)
        {
            Console.Error.WriteLine($"Package not found: {packageFile.FullName}");
            return ExitCodes.UsageError;
        }

        using var package = PluginPackage.Open(packageFile.FullName);
        var plugins = MetadataPluginReader.Read(package.AssemblyPaths);
        var builtAt = File.GetLastWriteTime(packageFile.FullName);

        Console.WriteLine($"Package {package.Id} {package.Version} ({package.AssemblyPaths.Count} assemblies, built {builtAt:yyyy-MM-dd HH:mm:ss})");
        Console.WriteLine();

        foreach (var plugin in plugins)
        {
            Console.WriteLine(plugin.TypeName);

            foreach (var step in plugin.Steps)
            {
                var filtering = step.FilteringAttributes.Count > 0
                    ? $" | filtering: {string.Join(", ", step.FilteringAttributes)}"
                    : string.Empty;
                Console.WriteLine($"  step  {step.Stage} {step.MessageName} of '{step.PrimaryEntityName ?? "any"}' ({step.Mode}){filtering}");
            }

            foreach (var image in plugin.Images)
            {
                var attributes = image.Attributes is null ? "all attributes" : string.Join(", ", image.Attributes);
                Console.WriteLine($"  image {image.ImageType} '{image.Alias}' ({attributes})");
            }
        }

        var errors = plugins.SelectMany(plugin => PluginStepValidator.Validate(plugin)).ToList();
        Console.WriteLine();

        if (errors.Count > 0)
        {
            Console.Error.WriteLine($"{errors.Count} error(s):");
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"  - {error}");
            }

            return ExitCodes.ValidationFailed;
        }

        Console.WriteLine($"OK: {plugins.Count} plug-in type(s), {plugins.Sum(p => p.Steps.Count)} step(s), no errors.");
        return ExitCodes.Success;
    }
}