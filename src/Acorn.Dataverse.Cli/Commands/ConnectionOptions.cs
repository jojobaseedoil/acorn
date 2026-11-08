using System.CommandLine;
using System.CommandLine.Parsing;

namespace Acorn.Dataverse.Cli.Commands;

internal static class ConnectionOptions
{
    public static Option<Uri> Environment() => new("--environment", "-e")
    {
        Description = "Dataverse environment URL, e.g. https://contoso.crm.dynamics.com",
        Required = true,
        CustomParser = ParseEnvironmentUrl,
    };

    private static Uri? ParseEnvironmentUrl(ArgumentResult result)
    {
        var value = result.Tokens.Single().Value;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            // Keeps only scheme + host: "https://org.crm.dynamics.com/main.aspx?..." also works.
            return new Uri(uri.GetLeftPart(UriPartial.Authority));
        }

        result.AddError($"'{value}' is not a valid environment URL. Expected something like https://contoso.crm.dynamics.com");
        return null;
    }

    public static Option<string?> Tenant() => new("--tenant", "-t")
    {
        Description = "Microsoft Entra tenant ID. Use it when your account has access to more than one tenant.",
    };
}