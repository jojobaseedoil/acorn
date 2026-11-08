using Acorn.Dataverse.Plugins;
using Acorn.Samples.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Acorn.Samples.Plugins;

public abstract class SamplePluginBase(string? unsecure, string? secure) : PluginBase(unsecure, secure)
{
    protected override void ConfigureServices(IServiceCollection services) => services.AddSampleServices();
}