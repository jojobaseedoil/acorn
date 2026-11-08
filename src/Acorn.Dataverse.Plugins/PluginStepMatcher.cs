using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins.Registration;

internal static class PluginStepMatcher
{
    public static bool Matches(PluginStepAttribute step, IPluginExecutionContext context) =>
        (int)step.Stage == context.Stage
        && (int)step.Mode == context.Mode
        && string.Equals(step.MessageName, context.MessageName, StringComparison.OrdinalIgnoreCase)
        && (step.PrimaryEntityName is null
            || string.Equals(step.PrimaryEntityName, context.PrimaryEntityName, StringComparison.OrdinalIgnoreCase));
}