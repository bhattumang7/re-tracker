# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for restore (layer-cache friendly)
COPY Tracker.sln .
COPY Tracker.Core/Tracker.Core.csproj          Tracker.Core/
COPY Tracker.Data/Tracker.Data.csproj          Tracker.Data/
COPY Tracker.Parsers/Tracker.Parsers.csproj    Tracker.Parsers/
COPY Tracker.Api/Tracker.Api.csproj            Tracker.Api/
COPY Tracker.Cli/Tracker.Cli.csproj            Tracker.Cli/
COPY Tracker.Tests/Tracker.Tests.csproj        Tracker.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish Tracker.Api/Tracker.Api.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tracker.Api.dll"]
