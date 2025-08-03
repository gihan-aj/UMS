# Stage 1: The Build Stage
# We use the official .NET 8 SDK image to build the app. It contains all the tools needed to build and test the application.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# --- Step 1: Restore Dependencies ---
# This is optimized for Docker's layer caching. We copy only the project files first and restore dependencies.
# If these files don't change, Docker will use the cached layer on subsequent builds, making it much faster.

# Copy the solution file
COPY UMS.sln .

COPY ["src/UMS.WebAPI/UMS.WebAPI.csproj", "src/UMS.WebAPI/"]

# Copy all the project files from the 'src' directory
COPY src/Mediator/Mediator.csproj ./src/Mediator/
COPY src/UMS.Application/UMS.Application.csproj ./src/UMS.Application/
COPY src/UMS.Domain/UMS.Domain.csproj ./src/UMS.Domain/
COPY src/UMS.Infrastructure/UMS.Infrastructure.csproj ./src/UMS.Infrastructure/
COPY src/UMS.SharedKernal/UMS.SharedKernel.csproj ./src/UMS.SharedKernal/
COPY src/UMS.WebAPI/UMS.WebAPI.csproj ./src/UMS.WebAPI/

# Copy all the project files from the 'tests' directory
COPY tests/UMS.API.IntergrationTests/UMS.API.IntergrationTests.csproj ./tests/UMS.API.IntergrationTests/
COPY tests/UMS.Application.UnitTests/UMS.Application.UnitTests.csproj ./tests/UMS.Application.UnitTests/
COPY tests/UMS.Domain.UnitTests/UMS.Domain.UnitTests.csproj ./tests/UMS.Domain.UnitTests/

# Restore all dependencies for the entire solution
RUN dotnet restore "UMS.sln"

# --- Step 2: Build & Publish the Application ---
# Now we copy the rest of the source code. Any changes to your C# files will invalidate the cache from this point onward.
COPY . .

# Run tests (optional but highly recommended for CI)
# This ensures that the code doesn't get published if tests are failing.
RUN dotnet test UMS.sln --no-restore

# Publish the WebAPI project, creating the final runnable files.
# We specify --no-restore because we've already done it.
RUN dotnet publish src/UMS.WebAPI/UMS.WebAPI.csproj -c Release -o /app/publish --no-restore

# Stage 2: The Final Image Stage
# We use the smaller ASP.NET runtime image because it's all we need to RUN the application.
# This makes the final image much smaller, more secure, and faster to download.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copy the published output from the 'build' stage into our final image
COPY --from=build /app/publish .

# The entry point command that will be executed when the container starts.
# This runs your .NET Web API.
ENTRYPOINT ["dotnet", "UMS.WebAPI.dll"]