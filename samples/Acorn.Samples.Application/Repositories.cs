using Acorn.Dataverse.Core;
using Acorn.Samples.Model;
using Microsoft.Xrm.Sdk;

namespace Acorn.Samples.Application;

public interface IAccountRepository
{
    Account GetForCreditLog(Guid id);
}

public sealed class AccountRepository(IOrganizationService service)
    : DataverseRepository<Account>(service), IAccountRepository
{
    public Account GetForCreditLog(Guid id) => Get(id, "name");
}

public interface INoteRepository
{
    Guid Create(Annotation note);
}

public sealed class NoteRepository(IOrganizationService service)
    : DataverseRepository<Annotation>(service), INoteRepository
{
}