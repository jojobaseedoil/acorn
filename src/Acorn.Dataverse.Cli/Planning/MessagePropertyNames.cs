namespace Acorn.Dataverse.Cli.Planning;

/// <summary>Default message property that identifies the record in an entity image, per message.</summary>
internal static class MessagePropertyNames
{
    public static string For(string messageName) => messageName.ToUpperInvariant() switch
    {
        "CREATE" => "Id",
        "SETSTATE" or "SETSTATEDYNAMICENTITY" => "EntityMoniker",
        "SEND" or "DELIVERINCOMING" or "DELIVERPROMOTE" => "EmailId",
        _ => "Target",
    };
}