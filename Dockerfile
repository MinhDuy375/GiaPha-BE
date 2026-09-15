FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["LacVietGenealogy.API/LacVietGenealogy.API.csproj", "LacVietGenealogy.API/"]
COPY ["LacVietGenealogy.Core/LacVietGenealogy.Core.csproj", "LacVietGenealogy.Core/"]
COPY ["LacVietGenealogy.Infrastructure/LacVietGenealogy.Infrastructure.csproj", "LacVietGenealogy.Infrastructure/"]

RUN dotnet restore "LacVietGenealogy.API/LacVietGenealogy.API.csproj"

COPY . .
WORKDIR "/src/LacVietGenealogy.API"
RUN dotnet publish "LacVietGenealogy.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:${PORT}
EXPOSE 8080

ENTRYPOINT ["dotnet", "LacVietGenealogy.API.dll"]
