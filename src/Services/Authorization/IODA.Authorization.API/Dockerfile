# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/Shared/ src/Shared/
COPY src/Services/Authorization/ src/Services/Authorization/

RUN dotnet restore src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj
RUN dotnet publish src/Services/Authorization/IODA.Authorization.API/IODA.Authorization.API.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IODA.Authorization.API.dll"]
