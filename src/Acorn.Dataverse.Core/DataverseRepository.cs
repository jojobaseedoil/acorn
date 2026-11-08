using System.ServiceModel;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Acorn.Dataverse.Core;

/// <summary>
/// Base class for Dataverse repositories. Derive one repository per entity and expose business-named
/// methods through an interface that services depend on. Reads are protected: only the repository
/// decides queries and columns. Writes are public and always send plain <see cref="Entity"/> instances.
/// </summary>
/// <typeparam name="T">Early-bound entity type, or <see cref="Entity"/> for late-bound repositories.</typeparam>
public abstract class DataverseRepository<T> where T : Entity, new()
{
    private const int ObjectDoesNotExistErrorCode = unchecked((int)0x80040217);

    /// <summary>Creates an early-bound repository. The logical name comes from <typeparamref name="T"/>.</summary>
    protected DataverseRepository(IOrganizationService service)
        : this(service, new T().LogicalName)
    {
    }

    /// <summary>Creates a repository for the given entity. Required for late-bound repositories.</summary>
    protected DataverseRepository(IOrganizationService service, string entityLogicalName)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));

        if (string.IsNullOrWhiteSpace(entityLogicalName))
        {
            throw new ArgumentException(
                "The entity logical name is required. Late-bound repositories must pass it to the constructor.",
                nameof(entityLogicalName));
        }

        EntityLogicalName = entityLogicalName;
    }

    /// <summary>Organization service used by the repository.</summary>
    protected IOrganizationService Service { get; }

    /// <summary>Logical name of the entity handled by the repository.</summary>
    protected string EntityLogicalName { get; }

    /// <summary>Page size used by <see cref="QueryAll"/>. Defaults to 5000, the Dataverse maximum.</summary>
    protected virtual int PageSize => 5000;

    /// <summary>Creates the record. The entity is sent as a plain <see cref="Entity"/>.</summary>
    public Guid Create(T entity) => Service.Create(ToPlain(entity));

    /// <summary>
    /// Updates the record with every attribute present in <paramref name="entity"/>.
    /// Send only changed attributes: updating a fully retrieved record rewrites all columns, triggers
    /// plug-ins filtered on unchanged attributes and pollutes auditing. Prefer <see cref="Patch"/>.
    /// </summary>
    public void Update(T entity) => Service.Update(ToPlain(entity));

    /// <summary>
    /// Updates only the attributes set by <paramref name="applyChanges"/>, on a new instance that holds
    /// nothing but the id and those changes.
    /// </summary>
    public void Patch(Guid id, Action<T> applyChanges)
    {
        Guard.NotNull(applyChanges);

        var entity = new T { Id = id };
        if (string.IsNullOrEmpty(entity.LogicalName))
        {
            entity.LogicalName = EntityLogicalName;
        }

        applyChanges(entity);
        Service.Update(ToPlain(entity));
    }

    /// <summary>Deletes the record.</summary>
    public void Delete(Guid id) => Service.Delete(EntityLogicalName, id);

    /// <summary>Retrieves a record with the given columns.</summary>
    /// <exception cref="RecordNotFoundException">The record does not exist.</exception>
    protected T Get(Guid id, params string[] columns)
        => RetrieveOrDefault(id, ToColumnSet(columns)) ?? throw new RecordNotFoundException(EntityLogicalName, id);

    /// <summary>Retrieves a record with the given columns, or <see langword="null"/> if it does not exist.</summary>
    protected T? Find(Guid id, params string[] columns) => RetrieveOrDefault(id, ToColumnSet(columns));

    /// <summary>Retrieves a record with all columns. Costly: prefer <see cref="Get"/> with explicit columns.</summary>
    /// <exception cref="RecordNotFoundException">The record does not exist.</exception>
    protected T GetWithAllColumns(Guid id)
        => RetrieveOrDefault(id, new ColumnSet(true)) ?? throw new RecordNotFoundException(EntityLogicalName, id);

    /// <summary>Creates a query for the entity with the given columns.</summary>
    protected QueryExpression CreateQuery(params string[] columns)
        => new(EntityLogicalName) { ColumnSet = ToColumnSet(columns) };

    /// <summary>Runs the query and returns every matching record, reading all pages.</summary>
    /// <remarks>
    /// Sets <see cref="QueryExpression.PageInfo"/> on the query. When <see cref="QueryExpression.TopCount"/>
    /// is set, a single request is made.
    /// </remarks>
    protected IReadOnlyList<T> QueryAll(QueryExpression query)
    {
        EnsureQueryForEntity(query);

        if (query.TopCount.HasValue)
        {
            return ToTyped(Service.RetrieveMultiple(query));
        }

        var results = new List<T>();
        query.PageInfo = new PagingInfo { Count = PageSize, PageNumber = 1 };

        while (true)
        {
            var page = Service.RetrieveMultiple(query);
            results.AddRange(ToTyped(page));

            if (!page.MoreRecords)
            {
                return results;
            }

            query.PageInfo.PageNumber++;
            query.PageInfo.PagingCookie = page.PagingCookie;
        }
    }

    /// <summary>Returns the first record matching the query, or <see langword="null"/>. Sets TopCount = 1 on the query.</summary>
    protected T? QueryFirstOrDefault(QueryExpression query)
    {
        EnsureQueryForEntity(query);
        query.TopCount = 1;
        query.PageInfo = new PagingInfo();

        return ToTyped(Service.RetrieveMultiple(query)).FirstOrDefault();
    }

    private T? RetrieveOrDefault(Guid id, ColumnSet columns)
    {
        try
        {
            return Service.Retrieve(EntityLogicalName, id, columns).ToEntity<T>();
        }
        catch (FaultException<OrganizationServiceFault> ex) when (ex.Detail?.ErrorCode == ObjectDoesNotExistErrorCode)
        {
            return null;
        }
    }

    private void EnsureQueryForEntity(QueryExpression query)
    {
        Guard.NotNull(query);

        if (!string.Equals(query.EntityName, EntityLogicalName, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"The query targets '{query.EntityName}', but this repository handles '{EntityLogicalName}'.",
                nameof(query));
        }
    }

    private static Entity ToPlain(T entity)
        => entity?.ToEntity<Entity>() ?? throw new ArgumentNullException(nameof(entity));

    private static ColumnSet ToColumnSet(string[]? columns) => new(columns ?? Array.Empty<string>());

    private static List<T> ToTyped(EntityCollection collection)
        => collection.Entities.Select(entity => entity.ToEntity<T>()).ToList();
}