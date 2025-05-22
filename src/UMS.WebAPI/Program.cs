using Mediator;
using UMS.Application;
using UMS.Application.Features.Users.Commands.RegisterUser;
using UMS.Infrastructure;
using UMS.WebAPI.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- Minimal API Endpoints ---

// POST /api/users/register
app.MapPost("/api/users/register", async (
    RegisterUserCommand command,
    ISender mediator // Inject ISender (from MediatR or your custom UMS.Mediator)
    ) =>
{
    var result = await mediator.Send(command);

    // Use the extension method to convert Result<Guid> to IResult
    return result.ToHttpResult(
        onSuccess: (userId) => Results.CreatedAtRoute( // Or Results.Ok(new { UserId = userId })
            routeName: "GetUserById", // Define this route if you have a GET endpoint for users
            routeValues: new { id = userId },
            value: new { UserId = userId }
        )
    );
})
.WithName("RegisterUser")
.WithTags("Users")
.Produces<object>(StatusCodes.Status201Created) // Success response type (adjust if needed)
.ProducesProblem(StatusCodes.Status400BadRequest) // For validation errors
.ProducesProblem(StatusCodes.Status409Conflict)   // For user already exists
.ProducesProblem(StatusCodes.Status500InternalServerError);


// Example: Placeholder for GetUserById (needed for CreatedAtRoute)
app.MapGet("/api/users/{id:guid}", (Guid id) =>
{
    // In a real app, you'd have a query to fetch the user
    return Results.Ok(new { Id = id, Message = "User details would be here (placeholder)" });
})
.WithName("GetUserById") // This name is used by CreatedAtRoute
.WithTags("Users");

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

app.Run();
