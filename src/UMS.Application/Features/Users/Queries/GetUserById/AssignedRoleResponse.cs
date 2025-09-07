namespace UMS.Application.Features.Users.Queries.GetUserById
{
    /// <summary>
    /// Represents a role assigned to the user.
    /// </summary>
    public sealed record AssignedRoleResponse(
        byte Id,
        string Name);
}
