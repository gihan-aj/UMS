using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Collections.Generic;
using System.Threading;
using UMS.Application.Features.Permissions.Queries.ListPermissions;
using UMS.Domain.Authorization;
using UMS.WebAPI.Common;

namespace UMS.WebAPI.Endpoints
{
    public static class PermissionEndpoints
    {
        public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new Asp.Versioning.ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var permissionGroup = app.MapGroup("/api/v{version:apiVersion}/permissions")
                .WithTags("Permissions")
                .WithApiVersionSet(apiVersionSet);

            // GET /api/v1/permissions
            permissionGroup.MapGet("/", async (
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new ListPermissionsQuery();
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Roles.Read)
                .WithName("ListPermissions")
                .Produces<List<PermissionGroupResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .MapToApiVersion(1, 0);

            return app;
        }
    }
}
