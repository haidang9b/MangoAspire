FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Orders.API/Orders.API.csproj", "Services/Orders.API/"]
COPY ["EventBus.RabbitMQ/EventBus.RabbitMQ.csproj", "EventBus.RabbitMQ/"]
COPY ["EventBus/EventBus.csproj", "EventBus/"]
COPY ["Shared/Mango.Core/Mango.Core.csproj", "Shared/Mango.Core/"]
COPY ["Shared/Mango.Events/Mango.Events.csproj", "Shared/Mango.Events/"]
COPY ["Shared/Mango.Infrastructure/Mango.Infrastructure.csproj", "Shared/Mango.Infrastructure/"]
COPY ["Shared/Mango.ServiceDefaults/Mango.ServiceDefaults.csproj", "Shared/Mango.ServiceDefaults/"]
RUN dotnet restore "./Services/Orders.API/Orders.API.csproj"
COPY . .
WORKDIR "/src/Services/Orders.API"
RUN dotnet build "./Orders.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Orders.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Orders.API.dll"]
