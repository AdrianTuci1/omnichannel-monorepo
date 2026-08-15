# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Recommender API (.NET 9, ASP.NET Core minimal API) — build multi-stage
# Context de build: integrations/recommender
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Directory.Build.props (local, duplicat din integrations/) furnizează
# TargetFramework=net9.0 — csproj-ul nu îl declară explicit.
COPY ["Directory.Build.props", "."]
COPY ["Recommender.Api.csproj", "."]
RUN dotnet restore "Recommender.Api.csproj"

COPY . .
RUN dotnet publish "Recommender.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5181
ENTRYPOINT ["dotnet", "Recommender.Api.dll"]
