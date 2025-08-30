namespace UMS.Application.Features.Users.Queries.GetUserById
{
    /// <summary>
    /// Represents a single, distinct permission the user has through their assigned roles.
    /// </summary>
    public sealed record PermissionDetailResponse(
        string PermissionName,
        string Description);
}
