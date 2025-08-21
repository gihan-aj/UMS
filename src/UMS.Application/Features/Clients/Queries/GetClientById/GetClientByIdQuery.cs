using System;
using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Clients.Queries.GetClientById
{
    public sealed record GetClientByIdQuery(Guid Id) : IQuery<ClientDetailsResponse>;
}
