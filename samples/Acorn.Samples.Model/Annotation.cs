using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Acorn.Samples.Model;

[EntityLogicalName(EntityLogicalName)]
public sealed class Annotation : Entity
{
    public const string EntityLogicalName = "annotation";

    public Annotation() : base(EntityLogicalName)
    {
    }

    [AttributeLogicalName("subject")]
    public string? Subject
    {
        get => GetAttributeValue<string>("subject");
        set => this["subject"] = value;
    }

    [AttributeLogicalName("notetext")]
    public string? NoteText
    {
        get => GetAttributeValue<string>("notetext");
        set => this["notetext"] = value;
    }

    [AttributeLogicalName("objectid")]
    public EntityReference? ObjectId
    {
        get => GetAttributeValue<EntityReference>("objectid");
        set => this["objectid"] = value;
    }
}