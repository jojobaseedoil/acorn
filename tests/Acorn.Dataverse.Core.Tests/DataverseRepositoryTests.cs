using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using NSubstitute;

namespace Acorn.Dataverse.Core.Tests;

public class DataverseRepositoryTests
{
    private const int ObjectDoesNotExist = unchecked((int)0x80040217);

    private readonly IOrganizationService _service = Substitute.For<IOrganizationService>();
    private readonly TestAccountRepository _repository;

    public DataverseRepositoryTests() => _repository = new TestAccountRepository(_service);

    [Fact]
    public void Get_RetrievesOnlyRequestedColumns()
    {
        var id = Guid.NewGuid();
        _service.Retrieve("account", id, Arg.Any<ColumnSet>()).Returns(new Entity("account", id) { ["name"] = "Contoso" });

        var account = _repository.GetName(id);

        Assert.Equal("Contoso", account.Name);
        _service.Received(1).Retrieve("account", id,
            Arg.Is<ColumnSet>(c => !c.AllColumns && c.Columns.Count == 1 && c.Columns[0] == "name"));
    }

    [Fact]
    public void Get_ThrowsRecordNotFound_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _service.Retrieve("account", id, Arg.Any<ColumnSet>()).Returns(_ => throw Fault(ObjectDoesNotExist));

        var ex = Assert.Throws<RecordNotFoundException>(() => _repository.GetName(id));

        Assert.Equal(id, ex.Id);
    }

    [Fact]
    public void Find_ReturnsNull_WhenRecordDoesNotExist()
    {
        _service.Retrieve("account", Arg.Any<Guid>(), Arg.Any<ColumnSet>()).Returns(_ => throw Fault(ObjectDoesNotExist));

        Assert.Null(_repository.FindName(Guid.NewGuid()));
    }

    [Fact]
    public void Find_Rethrows_OtherFaults()
    {
        _service.Retrieve("account", Arg.Any<Guid>(), Arg.Any<ColumnSet>()).Returns(_ => throw Fault(-1));

        Assert.Throws<FaultException<OrganizationServiceFault>>(() => _repository.FindName(Guid.NewGuid()));
    }

    [Fact]
    public void Create_SendsPlainEntity()
    {
        _repository.Create(new TestAccount { Name = "Contoso" });

        _service.Received(1).Create(Arg.Is<Entity>(e =>
            e.GetType() == typeof(Entity) && e.LogicalName == "account" && (string)e["name"] == "Contoso"));
    }

    [Fact]
    public void Patch_SendsOnlyChangedAttributes()
    {
        var id = Guid.NewGuid();

        _repository.Patch(id, account => account.Name = "Renamed");

        _service.Received(1).Update(Arg.Is<Entity>(e =>
            e.GetType() == typeof(Entity) && e.Id == id && e.Attributes.Count == 1 && (string)e["name"] == "Renamed"));
    }

    [Fact]
    public void QueryAll_ReadsEveryPage()
    {
        var firstPage = new EntityCollection(new List<Entity> { new("account", Guid.NewGuid()) })
        {
            MoreRecords = true,
            PagingCookie = "cookie-1",
        };
        var lastPage = new EntityCollection(new List<Entity> { new("account", Guid.NewGuid()) });
        _service.RetrieveMultiple(Arg.Any<QueryBase>()).Returns(firstPage, lastPage);

        var accounts = _repository.ActiveAccounts();

        Assert.Equal(2, accounts.Count);
        _service.Received(2).RetrieveMultiple(Arg.Any<QueryBase>());
    }

    [Fact]
    public void QueryAll_Throws_WhenQueryTargetsAnotherEntity()
        => Assert.Throws<ArgumentException>(() => _repository.Run(new QueryExpression("contact")));

    [Fact]
    public void Constructor_Throws_ForLateBoundRepositoryWithoutLogicalName()
        => Assert.Throws<ArgumentException>(() => new UnnamedLateBoundRepository(_service));

    [Fact]
    public void Patch_SetsLogicalName_ForLateBoundRepository()
    {
        var repository = new ContactRepository(_service);

        repository.Patch(Guid.NewGuid(), contact => contact["firstname"] = "Ana");

        _service.Received(1).Update(Arg.Is<Entity>(e => e.LogicalName == "contact"));
    }

    private static FaultException<OrganizationServiceFault> Fault(int errorCode)
        => new(new OrganizationServiceFault { ErrorCode = errorCode }, "Dataverse fault");

    [EntityLogicalName("account")]
    private sealed class TestAccount : Entity
    {
        public TestAccount() : base("account")
        {
        }

        public string? Name
        {
            get => GetAttributeValue<string>("name");
            set => this["name"] = value;
        }
    }

    private sealed class TestAccountRepository(IOrganizationService service) : DataverseRepository<TestAccount>(service)
    {
        public TestAccount GetName(Guid id) => Get(id, "name");

        public TestAccount? FindName(Guid id) => Find(id, "name");

        public IReadOnlyList<TestAccount> ActiveAccounts()
        {
            var query = CreateQuery("name");
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0);
            return QueryAll(query);
        }

        public IReadOnlyList<TestAccount> Run(QueryExpression query) => QueryAll(query);
    }

    private sealed class UnnamedLateBoundRepository(IOrganizationService service) : DataverseRepository<Entity>(service)
    {
    }

    private sealed class ContactRepository(IOrganizationService service) : DataverseRepository<Entity>(service, "contact")
    {
    }
}