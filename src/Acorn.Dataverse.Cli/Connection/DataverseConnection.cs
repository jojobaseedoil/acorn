using Azure.Core;
using Azure.Identity;
using Microsoft.PowerPlatform.Dataverse.Client;

namespace Acorn.Dataverse.Cli.Connection;

/// <summary>Connects to a Dataverse environment using Azure.Identity credentials. No secrets are stored.</summary>
internal static class DataverseConnection
{
    public static ServiceClient Connect(Uri environment, string? tenantId)
    {
        var credential = CreateCredential(tenantId);
        var scopes = new[] { new Uri(environment, "/.default").ToString() };

        var client = new ServiceClient(
            environment,
            async _ => (await credential.GetTokenAsync(new TokenRequestContext(scopes), CancellationToken.None)).Token,
            useUniqueInstance: true);

        if (!client.IsReady)
        {
            var error = client.LastError;
            var exception = client.LastException;
            client.Dispose();
            throw new InvalidOperationException($"Could not connect to {environment}: {error}", exception);
        }

        return client;
    }

    private static readonly string[] AzureCliFileNames = OperatingSystem.IsWindows()
        ? ["az.cmd", "az.exe"]
        : ["az"];

    // Order: environment variables (service principal), Azure CLI (local, and CI through azure/login with OIDC),
    // interactive browser (local fallback).
    private static ChainedTokenCredential CreateCredential(string? tenantId)
    {
        var credentials = new List<TokenCredential> { new EnvironmentCredential() };

        // Azure.Identity detects a missing Azure CLI by its English error message, which fails on localized
        // Windows and stops the chain. Only add the Azure CLI credential when the CLI is actually installed.
        if (IsAzureCliInstalled())
        {
            credentials.Add(new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }));
        }

        credentials.Add(new InteractiveBrowserCredential(new InteractiveBrowserCredentialOptions { TenantId = tenantId }));

        return new ChainedTokenCredential(credentials.ToArray());
    }

    private static bool IsAzureCliInstalled()
        => (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => AzureCliFileNames.Any(name => File.Exists(Path.Combine(directory, name))));
}