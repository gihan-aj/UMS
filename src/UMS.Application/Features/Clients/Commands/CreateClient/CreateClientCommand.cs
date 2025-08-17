using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Clients.Commands.CreateClient
{
    /// <summary>
    /// Command to register a new client application.
    /// </summary>
    public sealed record CreateClientCommand(
        string ClientId, // The public, unique ID for the client (e.g., "pos-system")
        string ClientName,
        List<string> RedirectUris) : ICommand<CreateClientResponse>;
}
