using Acorn.Dataverse.Core;
using Acorn.Samples.Model;
using Microsoft.Xrm.Sdk;

using System.Diagnostics.CodeAnalysis;

namespace Acorn.Samples.Application;

public sealed class CreditLimitService(IAccountRepository accounts, INoteRepository notes)
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
            throw new BusinessRuleException(
                $"Credit limit cannot increase more than 50% at once ({before.Value:N2} → {after.Value:N2}).",
                "CREDIT_LIMIT_INCREASE");
        }
    }

    public Guid LogChange(Guid accountId, Money? before, Money? after)
    {
        var account = accounts.GetForCreditLog(accountId);

        return notes.Create(new Annotation
        {
            Subject = "Credit limit changed",
            NoteText = $"{account.Name}: {before?.Value:N2} → {after?.Value:N2}",
            ObjectId = account.ToEntityReference(),
        });
    }
}