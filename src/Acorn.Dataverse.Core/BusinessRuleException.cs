namespace Acorn.Dataverse.Core;

/// <summary>A business rule was violated. Derive from it to create specific rule exceptions.</summary>
public class BusinessRuleException : DomainException
{
    /// <summary>Creates the exception with a default message.</summary>
    public BusinessRuleException() : base("A business rule was violated.")
    {
    }

    /// <summary>Creates the exception with a user-facing message.</summary>
    public BusinessRuleException(string message) : base(message)
    {
    }

    /// <summary>Creates the exception with a user-facing message and the underlying cause.</summary>
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Creates the exception with a user-facing message and a machine-readable code.</summary>
    public BusinessRuleException(string message, string? code) : base(message, code)
    {
    }
}