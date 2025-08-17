using System;

namespace UMS.Application.Features.Clients.Commands.CreateClient
{
    /// <summary>
    /// Response after creating a new client.
    /// IMPORTANT: The ClientSecret is only returned this one time. It should be stored securely.
    /// </summary>
    public sealed record CreateClientResponse(
        Guid Id,
        string ClientId,
        string ClientName,
        string ClientSecret); // The raw, unhashed secret
}
