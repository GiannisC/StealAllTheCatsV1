# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files and restore
COPY StealAllTheCats/*.csproj ./StealAllTheCats/
COPY StealAllTheCats.Tests/*.csproj ./StealAllTheCats.Tests/
COPY StealAllTheCats.sln ./
RUN dotnet restore

# Copy everything else
COPY . ./
RUN dotnet publish -c Release -o out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000
ENTRYPOINT ["dotnet", "StealAllTheCats.dll"]
