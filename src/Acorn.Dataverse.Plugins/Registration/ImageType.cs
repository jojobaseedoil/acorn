namespace Acorn.Dataverse.Plugins.Registration;

/// <summary>Type of an entity image.</summary>
public enum ImageType
{
    /// <summary>Snapshot of the record before the operation.</summary>
    PreImage = 0,

    /// <summary>Snapshot of the record after the operation. PostOperation only.</summary>
    PostImage = 1,

    /// <summary>Both pre and post images.</summary>
    Both = 2,
}