using System.CommandLine;
using Acorn.Dataverse.Cli.Connection;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Acorn.Dataverse.Cli.Commands;

internal static class WhoAmICommand
{
    public static Command Create()
    {
        var environmentOption = ConnectionOptions.Environment();
        var tenantOption = ConnectionOptions.Tenant();

        var command = new Command("whoami", "Connects to the environment and shows the organization and the signed-in user.")
        {
            environmentOption,
            tenantOption,
        };

        command.SetAction((parseResult, cancellationToken) => RunAsync(
            parseResult.GetValue(environmentOption)!,
            parseResult.GetValue(tenantOption),
            cancellationToken));

        return command;
    }

    private static async Task<int> RunAsync(Uri environment, string? tenantId, CancellationToken cancellationToken)
    {
        using var client = DataverseConnection.Connect(environment, tenantId);

        var whoAmI = (WhoAmIResponse)await client.ExecuteAsync(new WhoAmIRequest(), cancellationToken);
        var user = await client.RetrieveAsync(
            "systemuser", whoAmI.UserId, new ColumnSet("fullname", "domainname"), cancellationToken);

        Console.WriteLine($"Organization: {client.ConnectedOrgFriendlyName} ({client.ConnectedOrgVersion})");
        Console.WriteLine($"User:         {user.GetAttributeValue<string>("fullname")} <{user.GetAttributeValue<string>("domainname")}>");
        return ExitCodes.Success;
    }
}