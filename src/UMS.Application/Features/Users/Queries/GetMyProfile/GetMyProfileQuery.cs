using UMS.Application.Common.Messaging.Queries;

namespace UMS.Application.Features.Users.Queries.GetMyProfile
{
    public sealed record GetMyProfileQuery() : IQuery<UserProfileResponse>;
}
