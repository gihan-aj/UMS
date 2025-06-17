using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Threading;
using UMS.Application.Features.Roles.Queries.ListQueries;
using UMS.Domain.Authorization;
using UMS.SharedKernel;
using UMS.WebAPI.Common;

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

            return app;
        }
    }
}
