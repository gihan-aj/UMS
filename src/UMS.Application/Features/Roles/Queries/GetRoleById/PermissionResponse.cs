namespace UMS.Application.Features.Roles.Queries.GetRoleById
{
    /// <summary>
    /// Response DTO for a single permission.
    /// </summary>
    public sealed record PermissionResponse(short Id, string Name);
}
