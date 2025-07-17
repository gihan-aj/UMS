using Asp.Versioning;
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
using UMS.Application.Features.Users.Commands.SetInitialPassword;
using UMS.Application.Settings;
using UMS.WebAPI.Common;
using UMS.WebAPI.Contracts.Responses.Users;

namespace UMS.WebAPI.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthApiEndpoints(this IEndpointRouteBuilder app)
        {
            var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

            var authGroup = app.MapGroup("/api/v{verison:apiVersion}/auth")
                .WithTags("Authentication")
                .WithApiVersionSet(apiVersionSet);

            // POST /api/v1/auth/register
            authGroup.MapPost("/register", async (
                RegisterUserCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                // On success, we don't return the User ID directly for this flow.
                // We just confirm that the process has started.
                return result.ToHttpResult(onSuccess: (userId) => Results.Ok("Registration process started. Please check your email to activate your account."));
            })
                .WithName("RegisterUser")
                .MapToApiVersion(1,0);

            // POST /api/v1/auth/login
            authGroup.MapPost("/login", async (
                LoginUserCommand command,
                ISender mediator,
                HttpContext httpContext,
                IOptions<TokenSettings> tokenSettingOptions,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                if (result.IsFailure)
                {
                    return result.ToHttpResult();
                }

                // On success, set the refresh token in a secure HttpOnly cookie
                SetRefreshTokenCookie(
                    httpContext,
                    result.Value.RefreshToken,
                    tokenSettingOptions.Value);

                var response = new UserLoginResponse(
                    result.Value.UserId,
                    result.Value.Email,
                    result.Value.UserCode,
                    result.Value.Token,
                    result.Value.TokenExpiryUtc);

                return Results.Ok(response);
            })
                .WithName("LoginUser")
                .Produces<UserLoginResponse>(StatusCodes.Status200OK) // Success response type
                .ProducesProblem(StatusCodes.Status400BadRequest)   // For validation errors
                .ProducesProblem(StatusCodes.Status401Unauthorized) // For invalid credentials or inactive account
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .MapToApiVersion(1, 0);

            // POST /api/v1/auth/logout
            authGroup.MapPost("/logout", async (
                ISender mediator,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (!httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
                {
                    // No cookie, so the user is effectively logged out
                    return Results.NoContent();
                }

                var command = new LogoutUserCommand(refreshToken);
                await mediator.Send(command, cancellationToken);

                // Clear the cookie on the client browser
                httpContext.Response.Cookies.Delete("refreshToken");

                return Results.NoContent();
            })
                .RequireAuthorization() // Must be logged in (with a valid access token) to log out
                .WithName("LogoutUser")
                .Produces(StatusCodes.Status204NoContent)
                .ProducesProblem(StatusCodes.Status401Unauthorized)
                .MapToApiVersion(1, 0);

            // POST /api/v1/auth/refresh-token
            authGroup.MapPost("/refresh-token", async (
                ISender mediator,
                HttpContext httpContext,
                IOptions<TokenSettings> tokenSettingOptions,
                CancellationToken cancellationToken) =>
            {
                // Read the refresh token from the incoming cookie
                if (!httpContext.Request.Cookies.TryGetValue("refreshToken", out var refreshToken))
                {
                    return Results.Unauthorized();
                }

                var command = new RefreshTokenCommand(refreshToken);
                var result = await mediator.Send(command, cancellationToken);
                if (result.IsFailure)
                {
                    // If refresh fails, clear the cookie
                    httpContext.Response.Cookies.Delete("refreshToken");
                    return result.ToHttpResult();
                }

                // On success, set the new refresh token in the cookie
                SetRefreshTokenCookie(
                    httpContext,
                    result.Value.RefreshToken,
                    tokenSettingOptions.Value);

                // Return the new access token in the JSON body
                var response = new UserLoginResponse(
                    result.Value.UserId,
                    result.Value.Email,
                    result.Value.UserCode,
                    result.Value.Token,
                    result.Value.TokenExpiryUtc);

                return Results.Ok(response);
            })
                .WithName("RefreshToken")
                .Produces<UserLoginResponse>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status401Unauthorized) // For invalid/revoked tokens
                .MapToApiVersion(1, 0);

            // GET /api/v1/auth/activate?email=...&token=...
            authGroup.MapGet("/activate", async (
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

            // POST /api/v1/auth/resend-activation
            authGroup.MapPost("/resend-activation", async (
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

            // POST /api/v1/auth/request-password-reset
            authGroup.MapPost("/request-password-reset", async (
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

            // POST /api/v1/auth/reset-password
            authGroup.MapPost("/reset-password", async (
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

            // POST /api/v1/auth/set-initial-password
            authGroup.MapPost("/set-initial-password", async (
                SetInitialPasswordCommand command,
                ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.ToHttpResult(onSuccess: () => Results.Ok("Your password has been set and your account is now active."));
            })
                .WithName("SetInitialPassword")
                .MapToApiVersion(1, 0);

            return app;
        }

        private static void SetRefreshTokenCookie(
            HttpContext httpContext,
            string refreshToken,
            TokenSettings tokenSettings)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Prevent client-side script access
                Expires = DateTime.UtcNow.AddDays(tokenSettings.RefreshTokenExpiryDays),
                Secure = true, // Send only over HTTPS
                IsEssential = true, // Needed for auth
                SameSite = SameSiteMode.None // Or Lax, depending on the cross-site needs
            };

            httpContext.Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
