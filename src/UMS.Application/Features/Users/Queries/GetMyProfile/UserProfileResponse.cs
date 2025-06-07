using System;

namespace UMS.Application.Features.Users.Queries.GetMyProfile
{
    public sealed record UserProfileResponse(
        Guid Id,
        string UserCode,
        string Email,
        string? FirstName,
        string? LastName,
        bool IsActive,
        DateTime CreatedAtUtc,
        DateTime? LastLoginAtUtc);
}
