using System;
using System.Collections.Generic;

namespace UMS.Application.Features.Clients.Queries.GetClientById
{
    public sealed record ClientDetailsResponse(
        Guid Id,
        string ClientId,
        string ClientName,
        List<string> RedirectUris,
        DateTime CreatedAtUtc);
}
