namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Execution mode of a plug-in step.</summary>
public enum ExecutionMode
{
    /// <summary>Runs synchronously, inside the request.</summary>
    Synchronous = 0,

    /// <summary>Runs asynchronously, through the async service. PostOperation only.</summary>
    Asynchronous = 1,
}