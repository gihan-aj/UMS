using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using UMS.Application;
using UMS.Infrastructure;
using UMS.WebAPI.Endpoints;
using UMS.WebAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

// --- API Versioning Setup ---
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // Adds api-supported-versions and api-deprecated-versions headers
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader() // Reads version from URL segment (e.g., /api/v1/...)
                                         // new QueryStringApiVersionReader("api-version"), // Reads version from query string (?api-version=1.0)
                                         // new HeaderApiVersionReader("X-Api-Version")    // Reads version from HTTP header (X-Api-Version: 1.0)
    );
})
.AddApiExplorer(options => // Integrates with Swagger/OpenAPI
{
    // Format the version as "'v'major[.minor][-status]" (e.g., v1.0, v2, v1-beta)
    options.GroupNameFormat = "'v'VVV";
    // Substitute the version into the route template so Swagger UI can differentiate versions
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    // Swagger setup will be adjusted by AddApiExplorer for versioning
    // If you have custom Swagger options, keep them here.
    // Example: options.SwaggerDoc("v1", new OpenApiInfo { Title = "UMS API v1", Version = "v1" });
    // The AddApiExplorer will handle creating documents per API version.
});

builder.Services.AddLogging(configure => configure.AddConsole());

var app = builder.Build();

// --- Configure the HTTP request pipeline ---

// IMPORTANT: Register Global Exception Handling Middleware early in the pipeline.
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    // Swagger UI setup to display different API versions
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseSwaggerUI(options =>
    {
        foreach(var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.UseHttpsRedirection();

// ---- DEBUGGING CODE START ----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Adjust the namespace for IRequestHandler if it's not UMS.Mediator
        // This is the exact type your Mediator.Send method would try to resolve.
        var handlerTypeToResolve = typeof(Mediator.IRequestHandler<UMS.Application.Features.Users.Commands.RegisterUser.RegisterUserCommand, UMS.SharedKernel.Result<Guid>>);
        // Or, if IRequestHandler is in UMS.Application.Common.Messaging:
        // var handlerTypeToResolve = typeof(UMS.Application.Common.Messaging.IRequestHandler<UMS.Application.Users.Commands.RegisterUser.RegisterUserCommand, UMS.SharedKernel.Result.Result<Guid>>);


        var handlerInstance = services.GetService(handlerTypeToResolve);
        if (handlerInstance == null)
        {
            Console.WriteLine($"DEBUG: Handler for {handlerTypeToResolve.FullName} NOT FOUND in DI container.");
            // You can put a breakpoint here to inspect 'services'
        }
        else
        {
            Console.WriteLine($"DEBUG: Handler for {handlerTypeToResolve.FullName} FOUND: {handlerInstance.GetType().FullName}");
        }

        // Also try resolving the concrete type to see if it's registered at all
        var concreteHandler = services.GetService<UMS.Application.Features.Users.Commands.RegisterUser.RegisterUserCommandHandler>();
        if (concreteHandler == null)
        {
            Console.WriteLine($"DEBUG: Concrete handler RegisterUserCommandHandler NOT FOUND in DI container.");
        }
        else
        {
            Console.WriteLine($"DEBUG: Concrete handler RegisterUserCommandHandler FOUND.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DEBUG: Error during DI resolution check: {ex.Message}");
    }
}
// ---- DEBUGGING CODE END ----

// --- Map Organized Minimal API Endpoints ---
app.MapUserApiEndpoints();

app.Run();
