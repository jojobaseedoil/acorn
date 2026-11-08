using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Acorn.Dataverse;

internal static class Guard
{
    public static void NotNull(
        [NotNull] object? argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
#if NET
        ArgumentNullException.ThrowIfNull(argument, paramName);
#else
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
#endif
    }
}