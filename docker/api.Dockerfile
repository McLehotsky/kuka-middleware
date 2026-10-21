# KUKA FM Middleware API. Build context: repository root.
#   docker build -f docker/api.Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src

# Restore layer: only project/build metadata, so code changes don't invalidate the NuGet cache.
# Add the .csproj of every project the API references here.
COPY backend/global.json backend/Directory.Build.props ./
COPY backend/src/KukaMiddleware.Api/KukaMiddleware.Api.csproj src/KukaMiddleware.Api/
RUN dotnet restore src/KukaMiddleware.Api/KukaMiddleware.Api.csproj

COPY backend/ ./
RUN dotnet publish src/KukaMiddleware.Api/KukaMiddleware.Api.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish ./
# Non-root user provided by the aspnet image.
USER $APP_UID
ENTRYPOINT ["dotnet", "KukaMiddleware.Api.dll"]
