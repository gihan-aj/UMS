using System;
using UMS.Application.Common.Messaging.Commands;

namespace UMS.Application.Features.Clients.Commands.DeleteClient
{
    public sealed record DeleteClientCommand(Guid Id) : ICommand;
}
