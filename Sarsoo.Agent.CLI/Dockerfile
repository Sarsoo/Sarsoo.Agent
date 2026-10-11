# syntax=docker/dockerfile:1
# Learn about building .NET container images:
# https://github.com/dotnet/dotnet-docker/blob/main/samples/README.md
FROM mcr.microsoft.com/dotnet/sdk:10.0-resolute-aot AS build

WORKDIR /source
ARG BUILD_CONFIGURATION=Release

COPY ["Sarsoo.Agent.CLI/Sarsoo.Agent.CLI.csproj", "Sarsoo.Agent.CLI/"]
COPY ["Sarsoo.Agent/Sarsoo.Agent.csproj", "Sarsoo.Agent/"]
RUN dotnet restore "Sarsoo.Agent.CLI/Sarsoo.Agent.CLI.csproj"
COPY --link . .
WORKDIR "/source/Sarsoo.Agent.CLI"
RUN dotnet build "./Sarsoo.Agent.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Sarsoo.Agent.CLI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
    && rm /app/*.dbg /app/*.Development.json

# Final stage/image
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-resolute-chiseled
WORKDIR /app
COPY --link --from=publish /app/build /app
USER $APP_UID
ENTRYPOINT ["./AspNetCoreNativeAot"]