# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# CDP Worker (.NET 9, BackgroundService) — build multi-stage
# Context de build: integrations/cdp
# ---------------------------------------------------------------------------

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Cdp.Worker.csproj", "."]
RUN dotnet restore "Cdp.Worker.csproj"

COPY . .
RUN dotnet publish "Cdp.Worker.csproj" -c Release -o /app/publish --no-restore

# Worker fără ASP.NET — runtime (nu aspnet). DuckDB.NET.Data.Full include
# libduckdb.so pentru linux-x64, deci trebuie o bază cu glibc (nu alpine).
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Cdp.Worker.dll"]
