# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/Shared/ src/Shared/
COPY src/Services/Indexing/ src/Services/Indexing/

RUN dotnet restore src/Services/Indexing/IODA.Indexing.API/IODA.Indexing.API.csproj
RUN dotnet publish src/Services/Indexing/IODA.Indexing.API/IODA.Indexing.API.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IODA.Indexing.API.dll"]
