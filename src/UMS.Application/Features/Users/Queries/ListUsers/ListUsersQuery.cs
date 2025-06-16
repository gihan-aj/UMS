using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Users.Queries.GetMyProfile;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Queries.ListUsers
{
    /// <summary>
    /// Query to get a paginated list of all users.
    /// </summary>
    public sealed record ListUsersQuery(
        int Page,
        int PageSize,
        string? SerchTerm) : IQuery<PagedList<UserProfileResponse>>;
}
