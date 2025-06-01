using System;
using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UMS.WebAPI.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next, 
            ILogger<GlobalExceptionHandlingMiddleware> logger, 
            IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception has occured: {Message}", ex.Message);

                //If the response has alresdy started, don't attempt to rewrite it.
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("The response has already started, the error middleware will not execute.");
                    throw; // Re-throw the original exception
                }

                context.Response.ContentType = "application/json"; // Or "application/problem+json"
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                var errorId = Guid.NewGuid().ToString();  // Unique ID for this error instance for tracking

                // Create a standardized error response
                // In development, include more details. In production, keep it generic.
                object errorResponse;
                if (_env.IsDevelopment())
                {
                    errorResponse = new
                    {
                        errorId = errorId,
                        title = "An unexpected error occured.",
                        status = context.Response.StatusCode,
                        detail = ex.Message, // Full exception message in dev
                        stackTrace = ex.StackTrace // Stack trace in dev
                        // You could add more properties like ex.GetType().FullName
                    };
                }
                else
                {
                    errorResponse = new
                    {
                        errorId = errorId,
                        title = "An unexpected error occured.",
                        status = context.Response.StatusCode,
                        detail = "An internal server error occured. Please try again later or contact support with Error ID: " + errorId,
                    };
                }

                var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Ensure camelcase for JSON properties
                });

                await context.Response.WriteAsync(jsonResponse);
            }
        }
    }

    // Extension method to add the middleware to the HTTP request pipeline.
    public static class GlobalExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}
