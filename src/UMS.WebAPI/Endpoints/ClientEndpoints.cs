using System;
using System.Threading;
using Asp.Versioning;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using UMS.Application.Features.Clients.Commands.CreateClient;
using UMS.Application.Features.Clients.Commands.DeleteClient;
using UMS.Application.Features.Clients.Commands.UpdateClient;
using UMS.Application.Features.Clients.Queries.GetClientById;
using UMS.Application.Features.Clients.Queries.ListClients;
using UMS.Application.Features.Permissions.Commands.SyncPermissions;
using UMS.Domain.Authorization;
using UMS.SharedKernel;
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

            // GET /api/v1/clients
            clientGroup.MapGet("/", async (
                ISender mediator,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] string? searchTerm = null) =>
            {
                var query = new ListClientsQuery(page, pageSize, searchTerm);
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization(Permissions.Clients.Read) // You will need to add this permission
            .WithName("ListClients")
            .Produces<PagedList<ClientResponse>>(StatusCodes.Status200OK)
            .MapToApiVersion(1, 0);

            // GET /api/v1/clients/{id}
            clientGroup.MapGet("/{id:guid}", async (
                Guid id, 
                ISender mediator, 
                CancellationToken cancellationToken) =>
            {
                var query = new GetClientByIdQuery(id);
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
            .RequireAuthorization(Permissions.Clients.Read)
            .WithName("GetClientById")
            .Produces<ClientDetailsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .MapToApiVersion(1, 0);

            // PUT /api/v1/clients/{id}
            clientGroup.MapPut("/{id:guid}", async (
                Guid id,
                UpdateClientRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateClientCommand(id, request.ClientName, request.RedirectUris);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
            .RequireAuthorization(Permissions.Clients.Update)
            .WithName("UpdateClient")
            .Produces(StatusCodes.Status204NoContent)
            .MapToApiVersion(1, 0);

            // PUT /api/v1/{clientId}/permissions
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

            // DELETE /api/v1/{clientId}
            clientGroup.MapDelete("/{id:guid}", async (
                Guid id, 
                ISender mediator, 
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteClientCommand(id);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
            .RequireAuthorization(Permissions.Clients.Delete)
            .WithName("DeleteClient")
            .Produces(StatusCodes.Status204NoContent)
            .MapToApiVersion(1, 0);

            return app;
        }
    }
}
