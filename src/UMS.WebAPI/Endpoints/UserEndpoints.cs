using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Threading;
using UMS.Application.Features.Users.Commands.ActivateAccount;
using UMS.Application.Features.Users.Commands.AssignRole;
using UMS.Application.Features.Users.Commands.LoginUser;
using UMS.Application.Features.Users.Commands.RefreshToken;
using UMS.Application.Features.Users.Commands.RegisterUser;
using UMS.Application.Features.Users.Commands.RequestPasswordReset;
using UMS.Application.Features.Users.Commands.ResendActivationEmail;
using UMS.Application.Features.Users.Commands.ResetPassword;
using UMS.Application.Features.Users.Commands.SetRoles;
using UMS.Application.Features.Users.Queries.GetMyProfile;
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

            // POST /api/v1/users/register (or /api/v2/users/register if v2 is defined and requested)
            userGroup.MapPost("/register", async (
                RegisterUserCommand command, 
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(
                    onSuccess: (userId) => Results.CreatedAtRoute(
                        routeName: "GetUserById",
                        routeValues: new { Version = "1", id = userId }, // Ensure version is in routeValues for CreatedAtRoute
                        value: new { UserId = userId }
                        )
                    );
            })
                .WithName("RegisterUser")// Name should be unique across versions if route structure is the same
                                         // Or append version to name: .WithName("RegisterUser.V1")
                .Produces<object>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .MapToApiVersion(1, 0); // Explicitly map this endpoint to v1.0;

            // POST /api/v1/users/login
            userGroup.MapPost("/login", async (
                LoginUserCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                // Login response contains the token, so we return it directly on success.
                return result.ToHttpResult(onSuccess: Results.Ok);
            })
                .WithName("LoginUser")
                .Produces<LoginUserResponse>(StatusCodes.Status200OK) // Success response type
                .ProducesProblem(StatusCodes.Status400BadRequest)   // For validation errors
                .ProducesProblem(StatusCodes.Status401Unauthorized) // For invalid credentials or inactive account
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .MapToApiVersion(1, 0);

            // GET /api/v1/users/activate?email=...&token=...
            userGroup.MapGet("/activate", async (
                [FromQuery] string email,
                [FromQuery] string token,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new ActivateUserAccountCommand(email, token);
                var result = await mediator.Send(command, cancellationToken);

                // On success, you might redirect to a login page or a "success" page.
                // For an API, returning Ok() or a specific success message is common.
                // If returning HTML or redirecting:
                // if (result.IsSuccess) return Results.Redirect("/login?activated=true");
                // For now, just return the result as HTTP status.
                return result.ToHttpResult(onSuccess: () => Results.Ok("Account activated successfully. You can now log in."));
            })
                .WithName("ActivateUserAccount")
                .Produces(StatusCodes.Status200OK) // Success message
                .ProducesProblem(StatusCodes.Status400BadRequest) // For validation errors (e.g., missing token/email)
                .ProducesProblem(StatusCodes.Status404NotFound)   // For user not found
                .ProducesProblem(StatusCodes.Status422UnprocessableEntity, "application/json") // For invalid/expired token
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .MapToApiVersion(1, 0);

            // POST /api/v1/users/resend-activation
            userGroup.MapPost("/resend-activation", async (
                ResendActivationEmailCommand command, // Takes email from request body
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                // Even if the user is not found, or already active, we might return a generic success-like message
                // to avoid email enumeration. The handler's Result object should reflect this.
                // For now, mapping directly.
                return result.ToHttpResult(onSuccess: () => Results.Ok("If an account with this email exists and requires activation, a new activation email has been sent."));
            })
                .WithName("ResendActivationEmail")
                .Produces(StatusCodes.Status200OK) // For successful processing (even if user not found, for security)
                .ProducesProblem(StatusCodes.Status400BadRequest) // For validation errors (e.g., invalid email format)
                .ProducesProblem(StatusCodes.Status500InternalServerError) // For other failures
                .MapToApiVersion(1, 0);

            // POST /api/v1/users/request-password-reset
            userGroup.MapPost("/request-password-reset", async(
                RequestPasswordResetCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var rsult = await mediator.Send(command, cancellationToken);
                // For security, always return a generic success message to prvent email enumeration.
                return Results.Ok("If an account with this email exists, a password reset email has been sent.");
            })
                .WithName("RequestPasswordReset")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .MapToApiVersion(1, 0);

            // POST /api/v1/users/reset-password
            userGroup.MapPost("/reset-password", async (
                ResetPasswordCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                // On success, we just confirm the password has been reset.
                return result.ToHttpResult(onSuccess: () => Results.Ok("Your password has been reset successfully."));
            })
                .WithName("ResetPassword")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status422UnprocessableEntity) // For invalid/expired token
                .MapToApiVersion(1, 0);

            // POST /api/v1/users/refresh-token
            userGroup.MapPost("/refresh-token", async (
                RefreshTokenCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: Results.Ok);
            })
                .WithName("RefreshToken")
                .Produces<LoginUserResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // For invalid/revoked tokens
                .MapToApiVersion(1, 0);

            // ---- Protected Endpoints ----

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
