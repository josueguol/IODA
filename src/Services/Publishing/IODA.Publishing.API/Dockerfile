# Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/Shared/ src/Shared/
COPY src/Services/Publishing/ src/Services/Publishing/

RUN dotnet restore src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj
RUN dotnet publish src/Services/Publishing/IODA.Publishing.API/IODA.Publishing.API.csproj -c Release -o /app/publish --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IODA.Publishing.API.dll"]
