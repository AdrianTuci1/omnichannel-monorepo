# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Store API (.NET 9, ASP.NET Core minimal API) — build multi-stage
# Context de build: apps/store-api (conține store-api.sln, Directory.Build.props, src/)
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Manifeste de proiect + Directory.Build.props, pentru un layer de restore cache-abil.
# Directory.Build.props furnizează TargetFramework=net9.0 (csproj-urile nu îl declară).
COPY ["Directory.Build.props", "."]
COPY ["src/StoreApi.Domain/StoreApi.Domain.csproj", "src/StoreApi.Domain/"]
COPY ["src/StoreApi.Infrastructure/StoreApi.Infrastructure.csproj", "src/StoreApi.Infrastructure/"]
COPY ["src/StoreApi.Api/StoreApi.Api.csproj", "src/StoreApi.Api/"]
RUN dotnet restore "src/StoreApi.Api/StoreApi.Api.csproj"

# Restul surselor + publish (Release, fără restore — folosește layer-ul de mai sus)
COPY . .
RUN dotnet publish "src/StoreApi.Api/StoreApi.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5180
ENTRYPOINT ["dotnet", "StoreApi.Api.dll"]
