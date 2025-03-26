# Базовый образ для отладки (используется Visual Studio)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Сборка приложения
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NewServise/NewServise.csproj", "NewServise/"]
RUN dotnet restore "./NewServise/NewServise.csproj"
COPY . .
WORKDIR "/src/NewServise"
RUN dotnet build "./NewServise.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Публикация приложения
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NewServise.csproj" -c $BUILD_CONFIGURATION -o /app/publish

# Финальный образ (с ASP.NET Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NewServise.dll"]