using System;

namespace UMS.Application.Features.Clients.Queries.ListClients
{
    /// <summary>
    /// Response DTO for a client in a list.
    /// </summary>
    public sealed record ClientResponse(
        Guid Id,
        string ClientId,
        string ClientName,
        DateTime CreatedAtUtc);
}
