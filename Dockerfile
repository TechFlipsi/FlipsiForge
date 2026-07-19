# FlipsiForge Server — Full (mit KI + Web-UI)
# .NET 10.0.300 SDK, Single-File-Publish für linux-x64
# (c) 2026 TechFlipsi / Fabian Kirchweger — GPL-3.0

# === Build-Stage ===
FROM mcr.microsoft.com/dotnet/sdk:10.0.300 AS build
WORKDIR /src

# Restore zuerst (nur csproj — bessere Layer-Caching)
COPY src/FlipsiForge.Core/FlipsiForge.Core.csproj src/FlipsiForge.Core/
COPY src/FlipsiForge.Server/FlipsiForge.Server.csproj src/FlipsiForge.Server/
RUN dotnet restore src/FlipsiForge.Server/FlipsiForge.Server.csproj

# Rest kopieren und publish (Single-File, self-contained für linux-x64)
COPY src/ src/
RUN dotnet publish src/FlipsiForge.Server/FlipsiForge.Server.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    -p:PublishSingleFile=true \
    -p:IncludeContentFilesInSingleFile=true \
    -p:EnableCompressionInSingleFile=true \
    -o /app \
    --no-restore

# === Runtime-Stage (minimal debian-slim) ===
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime
WORKDIR /app

# Single-File-Binary + wwwroot (Content-Files sind im Single-File eingebettet)
COPY --from=build /app /app

# SQLite-Native-Library (wird von EF Core SQLite benötigt)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libsqlite3-0 \
    ca-certificates \
 && rm -rf /var/lib/apt/lists/*

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Server-Modus: Full (mit KI + Web-UI)
ENV FLIPSIFORGE_MODE=Full
ENV FLIPSIFORGE_AI=true
ENV FLIPSIFORGE_WEBUI=true

ENTRYPOINT ["/app/FlipsiForge.Server"]