using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using UMS.Application.Features.Users.Commands.ActivateAccount;
using UMS.Application.Features.Users.Commands.LoginUser;
using UMS.Application.Features.Users.Commands.LogoutUser;
using UMS.Application.Features.Users.Commands.RefreshToken;
using UMS.Application.Features.Users.Commands.RegisterUser;
using UMS.Application.Features.Users.Commands.RequestPasswordReset;
using UMS.Application.Features.Users.Commands.ResendActivationEmail;
using UMS.Application.Features.Users.Commands.ResetPassword;
using UMS.Application.Features.Users.Commands.SetRoles;
using UMS.Application.Features.Users.Queries.GetMyProfile;
using UMS.Application.Features.Users.Queries.ListUsers;
using UMS.Application.Settings;
using UMS.Domain.Authorization;
using UMS.SharedKernel;
using UMS.WebAPI.Common;
using UMS.WebAPI.Contracts.Requests.Users;
using UMS.WebAPI.Contracts.Responses.Users;

namespace UMS.WebAPI.Endpoints
{
    public static class UserEndpoints
    {
        public static IEndpointRouteBuilder MapUserApiEndpoints(this IEndpointRouteBuilder app)
        {
            // Define an API version set for this group of endpoints
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new Asp.Versioning.ApiVersion(1,0))
                // .HasApiVersion(new ApiVersion(2, 0)) // If you add a v2 later
                .ReportApiVersions()
                .Build();

            // Create a route group for user-related endpoints, now including versioning
            // The 'v{version:apiVersion}' segment will be replaced with the actual version (e.g., v1)
            var userGroup = app.MapGroup("/api/v{version:apiVersion}/users")
                .WithTags("Users")
                .WithApiVersionSet(apiVersionSet); // Associate with the version set

            // --- PROTECTED ENDPOINTS ---

            // GET /api/v1/users/me
            userGroup.MapGet("/me", async (
                ISender mediator,
                CancellationToken cancellationToken) => 
            {
                var query = new GetMyProfileQuery();
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization() // This makes the endpoint protected!
                .WithName("GetMyProfile")
                .Produces<UserProfileResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // If not authenticated
                .ProducesProblem(StatusCodes.Status404NotFound)   // If user from token not found in DB
                .MapToApiVersion(1, 0);

            // --- ADMIN ENDPOINTS ---

            // GET /api/v1/users
            userGroup.MapGet("/", async (  
                ISender mediator,
                CancellationToken cancellationToken,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10,
                [FromQuery] string? searchTerm = null) =>
            {
                var query = new ListUsersQuery(page, pageSize, searchTerm);
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Users.Read)
                .WithName("ListUsers")
                .Produces<PagedList<UserProfileResponse>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // Not authenticated
                .ProducesProblem(StatusCodes.Status403Forbidden)   // Authenticated but lacks permission
                .MapToApiVersion(1, 0);

            // GET /api/v1/users/{id}
            userGroup.MapGet("/{id:guid}", (Guid id /*, ApiVersion version - can be injected if needed */) =>
            {
                return Results.Ok(new { Id = id, Message = $"User details for API version would be here (placeholder)" });
            })
                .WithName("GetUserById") // Consider .WithName("GetUserById.V1")
                .Produces<object>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0); // Explicitly map this endpoint to v1.0;

            // POST /api/v1/users/{userId}/roles
            userGroup.MapPost("/{userId:guid}/roles", async (
                Guid userId,
                SetUserRolesRequest request, // A simple request body for the role ID
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new SetUserRolesCommand(userId, request.RoleIds);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
            .RequireAuthorization(Permissions.Users.AssignRole) // Protected
            .WithName("SetUserRoles")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .MapToApiVersion(1, 0);

            return app;
        }
    }
}
