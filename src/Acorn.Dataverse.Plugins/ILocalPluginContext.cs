using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins;

/// <summary>
/// Provides typed access to the Dataverse services available during a plug-in execution.
/// </summary>
public interface ILocalPluginContext
{
    /// <summary>The raw service provider supplied by Dataverse.</summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>The plug-in execution context.</summary>
    IPluginExecutionContext ExecutionContext { get; }

    /// <summary>The tracing service (Plug-in Trace Log).</summary>
    ITracingService TracingService { get; }

    /// <summary>Organization service running as the context user (<c>UserId</c>).</summary>
    IOrganizationService OrganizationService { get; }

    /// <summary>Organization service running as the initiating user (<c>InitiatingUserId</c>).</summary>
    IOrganizationService InitiatingUserService { get; }

    /// <summary>Organization service running as SYSTEM.</summary>
    IOrganizationService SystemService { get; }

    /// <summary>Writes a message to the trace log.</summary>
    void Trace(string message);
}