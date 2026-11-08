namespace Acorn.Dataverse.Plugins;

/// <summary>
/// Names of common Dataverse messages. For Custom APIs, use the API unique name.
/// </summary>
public static class MessageNames
{
    /// <summary>Create message.</summary>
    public const string Create = "Create";

    /// <summary>Update message.</summary>
    public const string Update = "Update";

    /// <summary>Delete message.</summary>
    public const string Delete = "Delete";

    /// <summary>Retrieve message.</summary>
    public const string Retrieve = "Retrieve";

    /// <summary>RetrieveMultiple message.</summary>
    public const string RetrieveMultiple = "RetrieveMultiple";

    /// <summary>Associate message (N:N relationships).</summary>
    public const string Associate = "Associate";

    /// <summary>Disassociate message (N:N relationships).</summary>
    public const string Disassociate = "Disassociate";

    /// <summary>Assign message.</summary>
    public const string Assign = "Assign";

    /// <summary>SetState message.</summary>
    public const string SetState = "SetState";

    /// <summary>SetStateDynamicEntity message.</summary>
    public const string SetStateDynamicEntity = "SetStateDynamicEntity";

    /// <summary>Merge message.</summary>
    public const string Merge = "Merge";
}