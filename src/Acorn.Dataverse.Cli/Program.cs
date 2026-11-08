using System.CommandLine;
using Acorn.Dataverse.Cli.Commands;

var root = new RootCommand("Acorn: deploys Dataverse plug-in packages from [PluginStep] and [PluginImage] declarations.")
{
    ValidateCommand.Create(),
};

return root.Parse(args).Invoke();