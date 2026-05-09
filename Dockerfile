# ── Stage 1: Build ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files first (Docker caches this layer)
COPY MarketMind.API/MarketMind.API.csproj             MarketMind.API/
COPY MarketMind.Application/MarketMind.Application.csproj   MarketMind.Application/
COPY MarketMind.Domain/MarketMind.Domain.csproj             MarketMind.Domain/
COPY MarketMind.Infrastructure/MarketMind.Infrastructure.csproj MarketMind.Infrastructure/

# Restore NuGet packages
RUN dotnet restore MarketMind.API/MarketMind.API.csproj

# Copy ALL source code
COPY . .

# Build and publish in Release mode
RUN dotnet publish MarketMind.API/MarketMind.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Render uses port 8080
EXPOSE 8080

# Start the API
ENTRYPOINT ["dotnet", "MarketMind.API.dll"]