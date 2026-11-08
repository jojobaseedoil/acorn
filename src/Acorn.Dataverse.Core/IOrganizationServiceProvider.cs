using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Core;

/// <summary>
/// Provides organization services for the identities available in the current execution.
/// Implemented by the plug-in runtime and by the Azure Functions integration.
/// </summary>
public interface IOrganizationServiceProvider
{
    /// <summary>
    /// Service running as the current user: the plug-in context user, or the application user in Azure Functions.
    /// This is also the default <see cref="IOrganizationService"/> injected by the container.
    /// </summary>
    IOrganizationService User { get; }

    /// <summary>Service running with elevated privileges (SYSTEM in plug-ins). Use only when required.</summary>
    IOrganizationService System { get; }
}