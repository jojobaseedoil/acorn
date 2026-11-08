namespace Acorn.Dataverse.Cli.Commands;

internal static class ErrorReporter
{
    /// <summary>
    /// Writes the outermost message (what failed) and the innermost one (why). The full exception,
    /// with every inner exception and stack trace, only with --verbose.
    /// </summary>
    public static void Report(Exception exception, bool verbose)
    {
        var root = exception;
        while (root.InnerException is not null)
        {
            root = root.InnerException;
        }

        Console.Error.WriteLine($"error: {exception.Message}");
        if (!ReferenceEquals(root, exception) && root.Message != exception.Message)
        {
            Console.Error.WriteLine($"  cause: {root.Message}");
        }

        if (verbose)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(exception);
        }
        else
        {
            Console.Error.WriteLine("Run again with --verbose for details.");
        }
    }
}