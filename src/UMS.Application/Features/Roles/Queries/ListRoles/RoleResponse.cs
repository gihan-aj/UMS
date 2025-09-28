namespace UMS.Application.Features.Roles.Queries.ListRoles
{
    /// <summary>
    /// Response DTO containing basic role information.
    /// </summary>
    public sealed record RoleResponse(
        byte Id,
        string Name,
        string? Description);
}
