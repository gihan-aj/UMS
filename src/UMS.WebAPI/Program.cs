using Microsoft.AspNetCore.Builder;
using System;
using System.Linq;
using UMS.Application;
using UMS.Infrastructure;
using UMS.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// --- Add services to the container ---
builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration)
    .AddPresentationServices(builder.Configuration);

var app = builder.Build();

// --- Check for the --seed-database command-line argument ---
if (args.Contains("--seed-database"))
{
    await app.UseInfrastructureServicesAsync();
    Console.WriteLine("Database seeding complete. Exiting.");
    return;
}

// --- Configure the HTTP request pipeline ---
app.ConfigurePipeline();

// --- Map API Endpoints ---
app.MapApiEndpoints();

app.Run();
