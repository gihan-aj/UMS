using System.Threading;
using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UMS.Application.Features.Clients.Commands.CreateClient;
using UMS.WebAPI.Common;

namespace UMS.WebAPI.Endpoints
{
    public static class ClientEndpoints
    {
        public static IEndpointRouteBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var clientGroup = app.MapGroup("/api/v{version:apiVersion}/clients")
                                 .WithTags("Clients")
                                 .WithApiVersionSet(apiVersionSet);

            // POST /api/v1/clients
            clientGroup.MapPost("/", async (
                CreateClientCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                // On success, the response contains the one-time secret
                return result.ToHttpResult();
            })
            .RequireAuthorization() // For now, just require authentication. Add permissions next.
            .WithName("CreateClient")
            .Produces<CreateClientResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .MapToApiVersion(1, 0);

            return app;
        }
    }
}
