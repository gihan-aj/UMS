using System;
using System.Collections.Generic;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Clients.Commands.UpdateClient
{
    public sealed record UpdateClientCommand(
        Guid Id, 
        string ClientName,
        List<string> RedirectUris): ICommand;
}
