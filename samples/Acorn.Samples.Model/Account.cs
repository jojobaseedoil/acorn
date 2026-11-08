using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;

namespace Acorn.Samples.Model;

[EntityLogicalName(EntityLogicalName)]
public sealed class Account : Entity
{
    public const string EntityLogicalName = "account";

    public Account() : base(EntityLogicalName)
    {
    }

    [AttributeLogicalName("name")]
    public string? Name
    {
        get => GetAttributeValue<string>("name");
        set => this["name"] = value;
    }

    [AttributeLogicalName("creditlimit")]
    public Money? CreditLimit
    {
        get => GetAttributeValue<Money>("creditlimit");
        set => this["creditlimit"] = value;
    }
}