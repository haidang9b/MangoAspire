# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/ChatAgent.App/ChatAgent.App.csproj", "Services/ChatAgent.App/"]
COPY ["EventBus.RabbitMQ/EventBus.RabbitMQ.csproj", "EventBus.RabbitMQ/"]
COPY ["EventBus/EventBus.csproj", "EventBus/"]
COPY ["Shared/Mango.Core/Mango.Core.csproj", "Shared/Mango.Core/"]
COPY ["Shared/Mediator/Mediator.csproj", "Shared/Mediator/"]
COPY ["Shared/Mango.Events/Mango.Events.csproj", "Shared/Mango.Events/"]
COPY ["Shared/Mango.Infrastructure/Mango.Infrastructure.csproj", "Shared/Mango.Infrastructure/"]
COPY ["Shared/Mango.RestApis/Mango.RestApis.csproj", "Shared/Mango.RestApis/"]
COPY ["Shared/Mango.ServiceDefaults/Mango.ServiceDefaults.csproj", "Shared/Mango.ServiceDefaults/"]
RUN dotnet restore "./Services/ChatAgent.App/ChatAgent.App.csproj"
COPY . .
WORKDIR "/src/Services/ChatAgent.App"
RUN dotnet build "./ChatAgent.App.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ChatAgent.App.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ChatAgent.App.dll"]