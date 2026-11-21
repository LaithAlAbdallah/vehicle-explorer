# Three stages: build the Angular app, publish the .NET API, then copy only the results into a small runtime image.
# Each stage first copies just the dependency files and installs, so Docker can reuse that layer
# when only source code changed.

# Stage 1: build the Angular app into static files.
FROM node:24-alpine AS web
WORKDIR /web
COPY src/web/package.json src/web/package-lock.json ./
RUN npm ci
COPY src/web/ ./
RUN npm run build

# Stage 2: publish the .NET API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/VehicleExplorer.Core/VehicleExplorer.Core.csproj src/VehicleExplorer.Core/
COPY src/VehicleExplorer.Infrastructure/VehicleExplorer.Infrastructure.csproj src/VehicleExplorer.Infrastructure/
COPY src/VehicleExplorer.Api/VehicleExplorer.Api.csproj src/VehicleExplorer.Api/
RUN dotnet restore src/VehicleExplorer.Api
COPY src/ src/
RUN dotnet publish src/VehicleExplorer.Api --no-restore --configuration Release --output /app

# Stage 3: the image that runs. It has the ASP.NET runtime only, no SDK and no Node.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /web/dist/web/browser ./wwwroot
# A non-root user that the official .NET images provide; the app listens on port 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "VehicleExplorer.Api.dll"]
