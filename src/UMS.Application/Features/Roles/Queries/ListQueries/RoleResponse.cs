namespace UMS.Application.Features.Roles.Queries.ListQueries
{
    /// <summary>
    /// Response DTO containing basic role information.
    /// </summary>
    public sealed record RoleResponse(
        byte Id,
        string Name);
}
