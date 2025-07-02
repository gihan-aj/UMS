# Dockerfile for .NET 8 Web API with a src/tests folder structure

# --- Stage 1: The 'build' stage ---
# We use the official .NET 8 SDK image to build the app.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the solution file and restore all dependencies for the entire solution.
# This is a key optimization for caching.
COPY ["UMS.sln", "."]
COPY ["src/UMS.WebAPI/UMS.WebAPI.csproj", "src/UMS.WebAPI/"]
# --- Add other projects from your 'src' folder here
COPY ["src/Mediator/Mediator.csproj", "src/Mediator/"]
COPY ["src/UMS.Application/UMS.Application.csproj", "src/UMS.Application/"]
COPY ["src/UMS.Domain/UMS.Domain.csproj", "src/UMS.Domain/"]
COPY ["src/UMS.Infrastructure/UMS.Infrastructure.csproj", "src/UMS.Infrastructure/"]
COPY ["src/UMS.SharedKernal/UMS.SharedKernel.csproj", "src/UMS.SharedKernal/"]
# --- Add your test projects from the 'tests' folder here
COPY ["tests/UMS.Application.UnitTests/UMS.Application.UnitTests.csproj", "tests/UMS.Application.UnitTests/"]
COPY ["tests/UMS.API.IntegrationTests/UMS.API.IntegrationTests.csproj", "tests/UMS.API.IntegrationTests/"]
RUN dotnet restore "UMS.sln"

# Copy the rest of the source code into the container
COPY . .

# Publish the specific web API project, placing the output in /app/publish
RUN dotnet publish "src/UMS.WebAPI/UMS.WebAPI.csproj" -c Release -o /app/publish

# --- Stage 2: The 'final' stage ---
# Use the much smaller and more secure ASP.NET runtime image to run the app.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy the published application files from the 'build' stage
COPY --from=build /app/publish .

# The command to run when the container starts.
ENTRYPOINT ["dotnet", "UMS.WebAPI.dll"]