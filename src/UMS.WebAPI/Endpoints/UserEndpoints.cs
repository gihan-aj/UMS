using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System;
using System.Threading;
using UMS.Application.Features.Users.Commands.ActivateAccount;
using UMS.Application.Features.Users.Commands.LoginUser;
using UMS.Application.Features.Users.Commands.RegisterUser;
using UMS.Application.Features.Users.Commands.ResendActivationEmail;
using UMS.WebAPI.Common;

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

            // GET /api/v1/users/{id}
            userGroup.MapGet("/{id:guid}", (Guid id /*, ApiVersion version - can be injected if needed */) =>
            {
                return Results.Ok(new { Id = id, Message = $"User details for API version would be here (placeholder)" });
            })
                .WithName("GetUserById") // Consider .WithName("GetUserById.V1")
                .Produces<object>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status404NotFound)
                .MapToApiVersion(1, 0); // Explicitly map this endpoint to v1.0;

            return app;
        }
    }
}
