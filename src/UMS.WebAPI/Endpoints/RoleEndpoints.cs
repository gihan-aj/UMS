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
using UMS.Application.Features.Roles.Queries.GetAllRoles;
using UMS.Application.Features.Roles.Queries.GetRoleById;
using UMS.Application.Features.Roles.Queries.ListRoles;
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

            // GET /api/v1/roles/list
            roleGroup.MapPost("/list", async (
                ISender mediator,
                [FromBody] PaginationQuery query,
                CancellationToken cancellationToken) =>
            {
                var request = new ListRolesQuery(query);
                var result = await mediator.Send(request, cancellationToken);
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
                var command = new CreateRoleCommand(request.Name, request.Description, request.PermissionNames);
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
            
            // GET /api/v1/roles/all
            roleGroup.MapGet("/all", async (
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var request = new GetAllRolesQuery();
                var result = await mediator.Send(request, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Roles.Read)
                .WithName("GetAllRoles")
                .Produces<RoleWithDetailedPermissionsResponse>(StatusCodes.Status200OK)
                .MapToApiVersion(1, 0);

            // PUT /api/v1/roles/{id}
            roleGroup.MapPut("/{id}", async (
                byte id,
                UpdateRoleRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateRoleCommand(id, request.Name, request.Description, request.PermissionNames);
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
