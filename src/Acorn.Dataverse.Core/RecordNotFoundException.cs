namespace Acorn.Dataverse.Core;

/// <summary>A required record was not found.</summary>
public sealed class RecordNotFoundException : DomainException
{
    /// <summary>Creates the exception with a default message.</summary>
    public RecordNotFoundException() : base("The record was not found.")
    {
    }

    /// <summary>Creates the exception with a user-facing message.</summary>
    public RecordNotFoundException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a user-facing message and the underlying cause.</summary>
    public RecordNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Creates the exception for a specific record.</summary>
    public RecordNotFoundException(string entityLogicalName, Guid id)
        : base($"Record '{id}' of '{entityLogicalName}' was not found.")
    {
        EntityLogicalName = entityLogicalName;
        Id = id;
    }

    /// <summary>Logical name of the entity, when known.</summary>
    public string? EntityLogicalName { get; }

    /// <summary>Id of the record, when known.</summary>
    public Guid? Id { get; }
}