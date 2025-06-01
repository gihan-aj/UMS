using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using UMS.Application.Features.Users.Commands.RegisterUser;
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
                ISender mediator) =>
            {
                var result = await mediator.Send(command);
                return result.ToHttpResult(
                    onSuccess: (userId) => Results.CreatedAtRoute(
                        routeName: "GetUserId",
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
