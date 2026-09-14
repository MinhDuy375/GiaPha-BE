# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching)
COPY LacVietGenealogy.sln .
COPY LacVietGenealogy.API/LacVietGenealogy.API.csproj LacVietGenealogy.API/
COPY LacVietGenealogy.Core/LacVietGenealogy.Core.csproj LacVietGenealogy.Core/
COPY LacVietGenealogy.Infrastructure/LacVietGenealogy.Infrastructure.csproj LacVietGenealogy.Infrastructure/

RUN dotnet restore

# Copy all source code and publish
COPY . .
RUN dotnet publish LacVietGenealogy.API/LacVietGenealogy.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "LacVietGenealogy.API.dll"]
