using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UMS.WebAPI.Endpoints;
using UMS.WebAPI.Middleware;

namespace UMS.WebAPI.Extensions
{
    public static class WebAppExtensions
    {
        public static WebApplication ConfigurePipeline(this WebApplication app)
        {
            app.UseGlobalExceptionHandling();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
                app.UseSwaggerUI(options =>
                {
                    foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
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
            app.UseCors("_myAllowSpecificOrigins");

            // Add IdentityServer to the pipeline. It handles its own routing.
            app.UseIdentityServer();

            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }

        public static WebApplication MapApiEndpoints(this WebApplication app)
        {
            app.MapAuthApiEndpoints();
            app.MapUserApiEndpoints();
            app.MapRoleApiEndpoints();
            app.MapPermissionApiEndpoints();
            app.MapClientApiEndpoints();

            return app;
        }
    }
}
