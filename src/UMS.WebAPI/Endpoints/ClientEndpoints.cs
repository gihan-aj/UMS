using System;
using System.Threading;
using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using UMS.Application.Features.Clients.Commands.CreateClient;
using UMS.Application.Features.Permissions.Commands.SyncPermissions;
using UMS.Domain.Authorization;
using UMS.WebAPI.Common;
using UMS.WebAPI.Contracts.Requests.Clients;

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
            .RequireAuthorization(Permissions.Clients.Create) 
            .WithName("CreateClient")
            .Produces<CreateClientResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .MapToApiVersion(1, 0);

            // POST /api/v1/{clientId}/permissions
            clientGroup.MapPut("/{clientId:guid}/permissions", async (
                Guid clientId,
                SyncPermissionsRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new SyncClientPermissionsCommand(clientId, request.PermissionNames);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
            .RequireAuthorization(Permissions.Clients.ManagePermissions)
            .WithName("SyncClientPermissions")
            .Produces(StatusCodes.Status204NoContent)
            .MapToApiVersion(1, 0);

            return app;
        }
    }
}
