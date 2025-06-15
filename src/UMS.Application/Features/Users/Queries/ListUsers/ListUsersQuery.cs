using System.Collections.Generic;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Users.Queries.GetMyProfile;

namespace UMS.Application.Features.Users.Queries.ListUsers
{
    /// <summary>
    /// Query to get a list of all users.
    /// </summary>
    public sealed record ListUsersQuery() : IQuery<List<UserProfileResponse>>;
}
