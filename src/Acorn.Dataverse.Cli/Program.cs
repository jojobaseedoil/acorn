using System.CommandLine;
using Acorn.Dataverse.Cli.Commands;

var verboseOption = new Option<bool>("--verbose")
{
    Description = "Shows full error details.",
    Recursive = true,
};

var root = new RootCommand("Acorn: deploys Dataverse plug-in packages from [PluginStep] and [PluginImage] declarations.")
{
    ValidateCommand.Create(),
    WhoAmICommand.Create(),
};
root.Options.Add(verboseOption);

var parseResult = root.Parse(args);

try
{
    return await parseResult.InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    ErrorReporter.Report(ex, parseResult.GetValue(verboseOption));
    return ExitCodes.Failure;
}