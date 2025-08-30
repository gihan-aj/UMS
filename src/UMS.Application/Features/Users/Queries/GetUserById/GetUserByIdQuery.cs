using System;
using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Users.Queries.GetUserById
{
    /// <summary>
    /// Query to get the detailed information for a single user by their ID.
    /// </summary>
    public sealed record GetUserByIdQuery(Guid UserId): IQuery<UserDetailResponse>;
}
