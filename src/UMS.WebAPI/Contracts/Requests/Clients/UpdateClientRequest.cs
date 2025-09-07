using System.Collections.Generic;

namespace UMS.WebAPI.Contracts.Requests.Clients
{
    public record UpdateClientRequest(string Name, List<string> RedirectUris);
}
