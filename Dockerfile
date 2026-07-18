# FlipsiForge Server — Full (mit KI + Web-UI)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/FlipsiForge.Core/FlipsiForge.Core.csproj src/FlipsiForge.Core/
COPY src/FlipsiForge.Server/FlipsiForge.Server.csproj src/FlipsiForge.Server/
RUN dotnet restore src/FlipsiForge.Server/FlipsiForge.Server.csproj

COPY src/ src/
RUN dotnet publish src/FlipsiForge.Server/FlipsiForge.Server.csproj \
    -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Server-Modus: Full (mit KI + Web-UI)
# Für Lite: ENV FLIPSIFORGE_MODE=Lite
ENV FLIPSIFORGE_MODE=Full
ENV FLIPSIFORGE_AI=true
ENV FLIPSIFORGE_WEBUI=true

ENTRYPOINT ["dotnet", "FlipsiForge.Server.dll"]