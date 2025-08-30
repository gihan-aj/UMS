using Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading;
using UMS.Application.Features.Users.Commands.ActivateUserbyAdmin;
using UMS.Application.Features.Users.Commands.CreateUserByAdmin;
using UMS.Application.Features.Users.Commands.DeactivateUserByAdmin;
using UMS.Application.Features.Users.Commands.DeleteUser;
using UMS.Application.Features.Users.Commands.SetRoles;
using UMS.Application.Features.Users.Commands.UpdateMyProfile;
using UMS.Application.Features.Users.Commands.UpdateUser;
using UMS.Application.Features.Users.Queries.GetMyProfile;
using UMS.Application.Features.Users.Queries.GetUserById;
using UMS.Application.Features.Users.Queries.ListUsers;
using UMS.Domain.Authorization;
using UMS.SharedKernel;
using UMS.WebAPI.Common;
using UMS.WebAPI.Contracts.Requests.Users;

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
                .RequireAuthorization(policy =>
                    {
                        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                        policy.RequireAuthenticatedUser();
                    }) // This makes the endpoint protected!
                .WithName("GetMyProfile")
                .Produces<UserProfileResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // If not authenticated
                .ProducesProblem(StatusCodes.Status404NotFound)   // If user from token not found in DB
                .MapToApiVersion(1, 0);
            
            // PUT /api/v1/users/me
            userGroup.MapPut("/me", async (
                UpdateUserRequest request,
                ISender mediator,
                CancellationToken cancellationToken) => 
            {
                var command = new UpdateMyProfileCommand(request.FirstName, request.LastName);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(policy =>
                    {
                        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                        policy.RequireAuthenticatedUser();
                    }) // This makes the endpoint protected!
                .WithName("UpdateMyProfile")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // If not authenticated
                .ProducesProblem(StatusCodes.Status404NotFound)   // If user from token not found in DB
                .MapToApiVersion(1, 0);

            // --- ADMIN ENDPOINTS ---

            // POST /api/v1/users
            userGroup.MapPost("/", async (
                CreateUserRequest request,
                ISender mediator,
                CancellationToken cancellationToken
                ) =>
            {
                var command = new CreateUserByAdminCommand(
                    request.Email, 
                    request.FirstName, 
                    request.LastName);

                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(
                    onSuccess: (userId) => Results.CreatedAtRoute("GetUserById", new { version = "1", id = userId }, new { Id = userId }));
            })
                .RequireAuthorization(Permissions.Users.Create)
                .WithName("CreateUserByAdmin")
                .Produces<Guid>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound)   
                .MapToApiVersion(1, 0);

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
            userGroup.MapGet("/{id:guid}", async (
                Guid id,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserByIdQuery(id);
                var result = await mediator.Send(query, cancellationToken);
                return result.ToHttpResult();
            })
                .RequireAuthorization(Permissions.Users.Read)
                .WithName("GetUserById")
                .Produces<UserDetailResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0); // Explicitly map this endpoint to v1.0;

            // PUT /api/v1/users/{id}
            userGroup.MapPut("/{id:guid}", async (
                Guid id,
                UpdateUserRequest request,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new UpdateUserCommand(id, request.FirstName, request.LastName);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Users.Update)
                .WithName("UpdateUser")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0);

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

            // POST /api/v1/users/{userId}/activate
            userGroup.MapPost("/{userId:guid}/activate", async (
                Guid userId,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new ActivateUserByAdminCommand(userId);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Users.ManageStatus)
                .WithName("ActivateUserByAdmin")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0);
            
            // POST /api/v1/users/{userId}/deactivate
            userGroup.MapPost("/{userId:guid}/deactivate", async (
                Guid userId,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeactivateUserByAdminCommand(userId);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Users.ManageStatus)
                .WithName("DeactivateUserByAdmin")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0);
            
            // DELETE /api/v1/users/{userId}
            userGroup.MapDelete("/{userId:guid}", async (
                Guid userId,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new DeleteUserCommand(userId);
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.NoContent());
            })
                .RequireAuthorization(Permissions.Users.Delete)
                .WithName("DeleteUser")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .ProducesProblem(StatusCodes.Status403Forbidden)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .MapToApiVersion(1, 0);

            return app;
        }
    }
}
