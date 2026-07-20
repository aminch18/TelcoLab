# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore against just the project files first, so layers cache when only code changes.
COPY TelcoLab.slnx ./
COPY src/TelcoLab.Domain/TelcoLab.Domain.csproj src/TelcoLab.Domain/
COPY src/TelcoLab.ClearingHouse.Contracts/TelcoLab.ClearingHouse.Contracts.csproj src/TelcoLab.ClearingHouse.Contracts/
COPY src/TelcoLab.Api/TelcoLab.Api.csproj src/TelcoLab.Api/
RUN dotnet restore src/TelcoLab.Api/TelcoLab.Api.csproj

COPY . .
RUN dotnet publish src/TelcoLab.Api/TelcoLab.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
# Run as the non-root user the base image provides.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelcoLab.Api.dll"]
