using Acorn.Samples.Model;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

using System.Diagnostics.CodeAnalysis;

namespace Acorn.Samples.Application;

public sealed class CreditLimitService(IOrganizationService service)
{
    private const decimal MaxIncreaseRatio = 1.5m;

    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "Instance member by design: resolved through dependency injection.")]
    public void ValidateChange(Money? before, Money? after)
    {
        if (before is null || after is null || before.Value <= 0)
        {
            return;
        }

        if (after.Value > before.Value * MaxIncreaseRatio)
        {
            throw new InvalidPluginExecutionException(
                $"Credit limit cannot increase more than 50% at once ({before.Value:N2} → {after.Value:N2}).");
        }
    }

    public Guid LogChange(Guid accountId, Money? before, Money? after)
    {
        // Read path: the server returns Entity, converted to early-bound locally.
        var account = service
            .Retrieve(Account.EntityLogicalName, accountId, new ColumnSet("name"))
            .ToEntity<Account>();

        var note = new Annotation
        {
            Subject = "Credit limit changed",
            NoteText = $"{account.Name}: {before?.Value:N2} → {after?.Value:N2}",
            ObjectId = account.ToEntityReference(),
        };

        // Write path: early-bound converted to plain Entity before reaching the server.
        return service.Create(note.ToEntity<Entity>());
    }
}