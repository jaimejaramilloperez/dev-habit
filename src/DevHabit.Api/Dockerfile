FROM mcr.microsoft.com/dotnet/aspnet:9.0.6-alpine3.21@sha256:ea72850bd81ba5c95ba88641a4fa315471bef9e3d1cd7e26c2594faff56e3a36 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0.301-alpine3.21@sha256:cec8f5d4537ff29112274379401142fa73d97fcc9f174dc1c623c29dcaef24c1 AS restore
WORKDIR /src
COPY ["nuget.config", "global.json", "Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/DevHabit.Api/DevHabit.Api.csproj", "./src/DevHabit.Api/"]
RUN dotnet restore "./src/DevHabit.Api/DevHabit.Api.csproj"

FROM restore AS build
ARG BUILD_CONFIGURATION=Release
COPY . .
RUN dotnet build "./src/DevHabit.Api/DevHabit.Api.csproj" \
  -c $BUILD_CONFIGURATION \
  --no-restore \
  --nologo \
  -p:OutputPath=/app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./src/DevHabit.Api/DevHabit.Api.csproj" \
  -c $BUILD_CONFIGURATION \
  --no-restore \
  --nologo \
  -p:PublishDir=/app/publish \
  -p:UseAppHost=false \
  -p:DebugType=None \
  -p:DebugSymbols=false
RUN rm /app/publish/appsettings.Development.json

FROM base AS prod
ENV ASPNETCORE_HTTP_PORTS=5000
EXPOSE $ASPNETCORE_HTTP_PORTS
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:$ASPNETCORE_HTTP_PORTS/health || exit 1
USER app
ENTRYPOINT ["dotnet", "DevHabit.Api.dll"]


FROM base AS dev
ENV ASPNETCORE_HTTP_PORTS=5000
ENV ASPNETCORE_HTTPS_PORTS=5001
EXPOSE $ASPNETCORE_HTTP_PORTS
EXPOSE $ASPNETCORE_HTTPS_PORTS
COPY --from=build /app/build .
HEALTHCHECK --interval=10s --timeout=5s --start-period=15s --retries=3 \
  CMD wget --no-verbose --tries=1 --no-check-certificate --spider https://localhost:$ASPNETCORE_HTTPS_PORTS/health || \
  wget --no-verbose --tries=1 --spider http://localhost:$ASPNETCORE_HTTP_PORTS/health || \
  exit 1
COPY ["src/DevHabit.Api/aspnetapp.pfx", "/https/aspnetapp.pfx"]
RUN chmod 644 /https/aspnetapp.pfx
ENTRYPOINT ["dotnet", "DevHabit.Api.dll"]
