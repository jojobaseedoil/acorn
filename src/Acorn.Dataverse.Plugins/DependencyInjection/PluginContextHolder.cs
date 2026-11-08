namespace Acorn.Dataverse.Plugins.DependencyInjection;

internal sealed class PluginContextHolder
{
    private ILocalPluginContext? _context;

    public ILocalPluginContext Context
    {
        get => _context ?? throw new InvalidOperationException("The plug-in context is not available in this scope.");
        set => _context = value;
    }
}