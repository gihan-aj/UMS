using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Clients
{
    public record UpdateClientRequest(string ClientName, List<string> RedirectUris);
}
