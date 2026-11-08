using System.Diagnostics;
using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins;

/// <summary>
/// Base class for Dataverse plug-ins. Handles context creation, tracing and exception handling.
/// </summary>
/// <remarks>
/// Dataverse caches and reuses plug-in instances across executions and threads.
/// Derived classes must not keep mutable state in fields.
/// </remarks>
public abstract class PluginBase : IPlugin
{
    /// <summary>Creates the plug-in without configuration.</summary>
    protected PluginBase() : this(null, null)
    {
    }

    /// <summary>Creates the plug-in with the step configuration provided by Dataverse.</summary>
    protected PluginBase(string? unsecureConfiguration, string? secureConfiguration)
    {
        UnsecureConfiguration = unsecureConfiguration;
        SecureConfiguration = secureConfiguration;
        PluginName = GetType().FullName ?? GetType().Name;
    }

    /// <summary>Full name of the concrete plug-in type, used in traces and error messages.</summary>
    protected string PluginName { get; }

    /// <summary>Unsecure configuration of the step.</summary>
    protected string? UnsecureConfiguration { get; }

    /// <summary>Secure configuration of the step.</summary>
    protected string? SecureConfiguration { get; }

    /// <inheritdoc />
    public void Execute(IServiceProvider serviceProvider)
    {
        var context = new LocalPluginContext(serviceProvider);
        var executionContext = context.ExecutionContext;

        context.Trace(
            $"Entered {PluginName} | {executionContext.MessageName} {executionContext.PrimaryEntityName} " +
            $"| Stage {executionContext.Stage} | Depth {executionContext.Depth} | Correlation {executionContext.CorrelationId}");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            ExecutePlugin(context);
        }
        catch (InvalidPluginExecutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            context.Trace(ex.ToString());
            throw new InvalidPluginExecutionException($"An unexpected error occurred in {PluginName}: {ex.Message}", ex);
        }
        finally
        {
            context.Trace($"Exiting {PluginName} | {stopwatch.ElapsedMilliseconds} ms");
        }
    }

    /// <summary>Implements the plug-in logic.</summary>
    protected abstract void ExecutePlugin(ILocalPluginContext context);
}