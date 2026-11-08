using Acorn.Dataverse.Core;
using Microsoft.Xrm.Sdk;

namespace Acorn.Dataverse.Plugins.DependencyInjection;

internal sealed class PluginOrganizationServiceProvider(ILocalPluginContext context) : IOrganizationServiceProvider
{
    public IOrganizationService User => context.OrganizationService;

    public IOrganizationService System => context.SystemService;
}