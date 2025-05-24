FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine3.21@sha256:30fdbd1b5963bba6ed66190d72d877b750d4203a671c9b54592f4551b8c5a087 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine3.21@sha256:2244f80ac7179b0feaf83ffca8fe82d31fbced5b7e353755bf9515a420eba711 AS restore
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

FROM base AS final
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --spider http://localhost:5000/health || exit 1
USER app
ENTRYPOINT ["dotnet", "DevHabit.Api.dll"]


FROM base AS dev
COPY --from=build /app/build .
HEALTHCHECK --interval=30s --timeout=30s --start-period=5s --retries=3 \
  CMD wget --no-verbose --tries=1 --no-check-certificate --spider http://localhost:5000/health || exit 1
EXPOSE 5001
COPY ["src/DevHabit.Api/aspnetapp.pfx", "/https/aspnetapp.pfx"]
RUN chmod 644 /https/aspnetapp.pfx
ENTRYPOINT ["dotnet", "DevHabit.Api.dll"]
