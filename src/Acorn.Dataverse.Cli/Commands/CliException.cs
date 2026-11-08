namespace Acorn.Dataverse.Cli.Commands;

/// <summary>An expected failure: the message is ready to show and carries its exit code. No stack trace.</summary>
internal sealed class CliException : Exception
{
    public CliException(int exitCode, string message) : base(message)
    {
        ExitCode = exitCode;
    }

    public int ExitCode { get; }
}