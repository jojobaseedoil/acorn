namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Stages of the Dataverse event execution pipeline.</summary>
public enum PipelineStage
{
    /// <summary>Before the main operation, outside the database transaction.</summary>
    PreValidation = 10,

    /// <summary>Before the main operation, inside the database transaction.</summary>
    PreOperation = 20,

    /// <summary>The main operation. Used by Custom API plug-ins.</summary>
    MainOperation = 30,

    /// <summary>After the main operation.</summary>
    PostOperation = 40,
}