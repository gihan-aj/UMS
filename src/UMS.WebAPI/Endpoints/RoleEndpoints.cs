using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading;
using UMS.Application.Features.Roles.Commands.AssignPermissions;
using UMS.Application.Features.Roles.Commands.CreateRole;
using UMS.Application.Features.Roles.Commands.DeleteRole;
using UMS.Application.Features.Roles.Commands.UpdateRole;
using UMS.Application.Features.Roles.Queries.GetRoleById;
using UMS.Application.Features.Roles.Queries.ListQueries;
using UMS.Domain.Authorization;
using UMS.SharedKernel;
using UMS.WebAPI.Common;
using UMS.WebAPI.Contracts.Requests.Roles;

namespace UMS.WebAPI.Endpoints
{
    public static class RoleEndpoints
    {
        public static IEndpointRouteBuilder MapRoleApiEndpoints(this IEndpointRouteBuilder app)
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new Asp.Versioning.ApiVersion(1,0))
                .ReportApiVersions()
                .Build();

            var roleGroup = app.MapGroup("/api/v{verison:apiVersion}/roles")
                .WithTags("Roles")
                .WithApiVersionSet(apiVersionSet);

            // GET /api/v1/roles
            roleGroup.MapGet("/", async (
                ISender mediator,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] string? searchTerm = null) =>
            {
                var query = new ListRolesQuery(page, pageSize, searchTerm);
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Roles.Read) // Protected by a permission
                .WithName("ListRoles")
                .Produces<PagedList<RoleResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .MapToApiVersion(1, 0);

            // POST /api/v1/roles
            roleGroup.MapPost("/", async (
                CreateRoleRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateRoleCommand(request.Name, request.PermissionNames);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(
                    onSuccess: (roleId) => Results.CreatedAtRoute(
                        "GetRoleById",
                        new { version = "1", id = roleId },
                        new { Id = roleId }
                    )
                );
            })
                .RequireAuthorization(Permissions.Roles.Create) // Protected by a permission
                .WithName("CreateRole")
                .Produces(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .MapToApiVersion(1, 0);

            // GET /api/v1/roles/{id}
            roleGroup.MapGet("/{id}", async (
                byte id,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetRoleByIdQuery(id);
                var result = await mediator.Send(request, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Roles.Read)
                .WithName("GetRoleById")
                .Produces<RoleWithPermissionsResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0);

            // PUT /api/v1/roles/{id}
            roleGroup.MapPut("/{id}", async (
                byte id,
                UpdateRoleRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateRoleCommand(id, request.Name, request.PermissionNames);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Roles.Update)
                .WithName("UpdateRole")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .MapToApiVersion(1, 0);

            // PUT /api/v1/roles/{id}/permissions
            roleGroup.MapPut("/{id}/permissions", async (
                byte id,
                AssignPermissionsRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new AssignPermissionsToRoleCommand(id, request.PermissionNames);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Roles.AssignPermissions)
                .WithName("AssignPermissionsToRole")    
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .MapToApiVersion(1, 0);

            // DELETE /api/v1/roles/{id}
            roleGroup.MapDelete("/{id}", async (
                byte id,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteRoleCommand(id);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Roles.Delete)
                .WithName("DeleteRole")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .MapToApiVersion(1, 0);

            return app;
        }
    }
}
