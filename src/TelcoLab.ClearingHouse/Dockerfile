# Build context is the repository root (see docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY TelcoLab.slnx ./
COPY src/TelcoLab.ClearingHouse.Contracts/TelcoLab.ClearingHouse.Contracts.csproj src/TelcoLab.ClearingHouse.Contracts/
COPY src/TelcoLab.ClearingHouse/TelcoLab.ClearingHouse.csproj src/TelcoLab.ClearingHouse/
RUN dotnet restore src/TelcoLab.ClearingHouse/TelcoLab.ClearingHouse.csproj

COPY . .
RUN dotnet publish src/TelcoLab.ClearingHouse/TelcoLab.ClearingHouse.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "TelcoLab.ClearingHouse.dll"]
