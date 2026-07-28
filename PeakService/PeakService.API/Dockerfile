# Contexto de build: la raíz del monorepo (peaker-app), para incluir common-library.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY common-library/ common-library/
COPY peak-service/ peak-service/

RUN dotnet restore peak-service/PeakService/PeakService.API/PeakService.API.csproj
RUN dotnet publish peak-service/PeakService/PeakService.API/PeakService.API.csproj \
    -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --no-restore

FROM build AS migrator-build
ARG BUILD_CONFIGURATION=Release
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet tool install --global dotnet-ef --version 10.0.10
RUN dotnet ef migrations bundle --self-contained --target-runtime linux-x64 \
    --configuration $BUILD_CONFIGURATION \
    --project peak-service/PeakService/PeakService.Infrastructure/PeakService.Infrastructure.csproj \
    --startup-project peak-service/PeakService/PeakService.API/PeakService.API.csproj \
    --output /app/efbundle

FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS migrator
WORKDIR /app
COPY --from=migrator-build /app/efbundle .
USER $APP_UID
ENTRYPOINT ["/bin/sh", "-c", "./efbundle --connection \"$ConnectionStrings__PeakDatabase\""]

FROM build AS worker-build
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish peak-service/PeakService/PeakService.Ingestion/PeakService.Ingestion.csproj \
    -c $BUILD_CONFIGURATION -o /app/ingestion /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS worker
WORKDIR /app
COPY --from=worker-build /app/ingestion .
USER $APP_UID
ENTRYPOINT ["dotnet", "PeakService.Ingestion.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "PeakService.API.dll"]
