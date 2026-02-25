FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["UI/Mango.Web/Mango.Web.csproj", "UI/Mango.Web/"]
COPY ["Shared/Mango.Core/Mango.Core.csproj", "Shared/Mango.Core/"]
COPY ["Shared/Mango.Infrastructure/Mango.Infrastructure.csproj", "Shared/Mango.Infrastructure/"]
COPY ["Shared/Mango.RestApis/Mango.RestApis.csproj", "Shared/Mango.RestApis/"]
COPY ["Shared/Mango.ServiceDefaults/Mango.ServiceDefaults.csproj", "Shared/Mango.ServiceDefaults/"]
RUN dotnet restore "./UI/Mango.Web/Mango.Web.csproj"
COPY . .
WORKDIR "/src/UI/Mango.Web"
RUN dotnet build "./Mango.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Mango.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Mango.Web.dll"]
