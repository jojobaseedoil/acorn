namespace Acorn.Dataverse.Core;

/// <summary>
/// Base class for expected, domain-level failures. The message is safe to show to the user.
/// Hosts translate it: plug-ins surface the message as-is; integrations treat it as a permanent failure.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Creates the exception without a message.</summary>
    protected DomainException()
    {
    }

    /// <summary>Creates the exception with a user-facing message.</summary>
    protected DomainException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a user-facing message and the underlying cause.</summary>
    protected DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Creates the exception with a user-facing message and a machine-readable code.</summary>
    protected DomainException(string message, string? code) : base(message)
    {
        Code = code;
    }

    /// <summary>Optional machine-readable error code, for integrations and logs.</summary>
    public string? Code { get; }
}