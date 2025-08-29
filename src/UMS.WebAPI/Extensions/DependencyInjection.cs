using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure.Authentication.Settings;
using UMS.WebAPI.Services;
using Asp.Versioning;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace UMS.WebAPI.Extensions
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPresentationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add HttpContextAccessor and custom services
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddControllersWithViews();

            // Configure JWT Authentication
            //var jwtSettings = new JwtSettings();
            //configuration.Bind(JwtSettings.SectionName, jwtSettings);
            //services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
            //{
            //    ValidateIssuer = true,
            //    ValidIssuer = jwtSettings.Issuer,
            //    ValidateAudience = true,
            //    ValidAudience = jwtSettings.Audience,
            //    ValidateLifetime = true,
            //    ValidateIssuerSigningKey = true,
            //    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            //    ClockSkew = TimeSpan.Zero
            //});

            // --- Authentication Setup (Dual Schemes) ---
            var jwtSettings = new JwtSettings();
            configuration.Bind(JwtSettings.SectionName, jwtSettings);

            // Set the default scheme to Cookie's for IdentityServer's interactive UI.
            services.AddAuthentication(IdentityConstants.ApplicationScheme)
                // Add the cookie handler for managing user sessions during login, logout, etc.
                .AddCookie(IdentityConstants.ApplicationScheme, options =>
                {
                    // Configure cookie options if needed
                    options.LoginPath = "/Account/Login"; // Example path to a login page
                })
                // Add the JWT Bearer handler for protecting APIs.
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            // Configure API Versioning
            services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

            // Configure Swagger
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "UMS API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy(name: "_myAllowSpecificOrigins", policy =>
                {
                    policy.WithOrigins("http://localhost:4200", "https://localhost:4200", "https://ums-client-200915304888.asia-south1.run.app")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                });
            });

            services.AddLogging(configure => configure.AddConsole());

            return services;
        }
    }
}
