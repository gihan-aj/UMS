using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;
using UMS.Application;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure;
using UMS.Infrastructure.Authentication.Settings;
using UMS.WebAPI.Endpoints;
using UMS.WebAPI.Middleware;
using UMS.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
// Add HttpContextAccessor, which is required by CurrentUserService
builder.Services.AddHttpContextAccessor();
// Register our CurrentUserService
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// --- JWT Authentication Setup ---
// Retrieve JwtSettings from configuration to use for token validation parameters
var jwtSettings = new JwtSettings();
builder.Configuration.Bind(JwtSettings.SectionName, jwtSettings);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateLifetime = true, // Check token expiry

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),

            ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew if desired
        };
    });

builder.Services.AddAuthorization(); // Add authorization services

// --- End JWT Authentication Setup ---

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
    // Add JWT Authentication support to swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer" // "bearer" in lowrecase is important 
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Swagger setup will be adjusted by AddApiExplorer for versioning
    // If you have custom Swagger options, keep them here.
    // Example: options.SwaggerDoc("v1", new OpenApiInfo { Title = "UMS API v1", Version = "v1" });
    // The AddApiExplorer will handle creating documents per API version.
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "UMS API", Version = "v1" });
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
        // DeveloperExceptionPage can still be useful for some specific scenarios during development,
        // but our custom middleware will handle most unhandled exceptions from the app logic.
        // app.UseDeveloperExceptionPage(); // You might keep or remove this depending on preference.
        // Our middleware provides a structured JSON response even in dev.
    });
}
else
{
    // For non-development environments, you might have other specific error handling
    // or rely solely on the global exception handler.
    // app.UseExceptionHandler("/Error"); // Example for MVC/Razor Pages
    // app.UseHsts(); // If using HSTS
}   

app.UseHttpsRedirection();

// IMPORTANT: Add Authentication and Authorization middleware
// UseAuthentication must come before UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// --- Map Organized Minimal API Endpoints ---
app.MapUserApiEndpoints();

// --- Run Database Seeder ---
// This will run the seeder every time the application starts.
// For production, you might want this to be a one-off command.
await app.UseInfrastructureServicesAsync();

app.Run();
