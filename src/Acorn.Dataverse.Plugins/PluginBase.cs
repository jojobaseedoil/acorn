using System.Diagnostics;
using System.Reflection;
using Acorn.Dataverse.Plugins.Registration;
using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins;

/// <summary>
/// Base class for Dataverse plug-ins. Handles context creation, tracing, exception handling
/// and checks the execution against the declared <see cref="PluginStepAttribute"/>s.
/// </summary>
/// <remarks>
/// Dataverse caches and reuses plug-in instances across executions and threads.
/// Derived classes must not keep mutable state in fields.
/// </remarks>
public abstract class PluginBase : IPlugin
{
    private readonly PluginStepAttribute[] _steps;

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
        _steps = GetType().GetCustomAttributes<PluginStepAttribute>(inherit: false).ToArray();
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
            $"| Stage {executionContext.Stage} | Mode {executionContext.Mode} | Depth {executionContext.Depth} " +
            $"| Correlation {executionContext.CorrelationId}");

        var stopwatch = Stopwatch.StartNew();
        try
        {
            EnsureDeclaredStep(executionContext);
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

    private void EnsureDeclaredStep(IPluginExecutionContext executionContext)
    {
        if (_steps.Length == 0 || _steps.Any(step => PluginStepMatcher.Matches(step, executionContext)))
        {
            return;
        }

        throw new InvalidPluginExecutionException(
            $"{PluginName} is not declared for {executionContext.MessageName} of '{executionContext.PrimaryEntityName}' " +
            $"at stage {executionContext.Stage} (mode {executionContext.Mode}). " +
            "Check the [PluginStep] attributes or the step registration in the environment.");
    }
}