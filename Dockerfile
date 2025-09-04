# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 8080
EXPOSE 8081

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .

# unit test and code coverage
RUN dotnet test KeeperData.Api.sln --filter Dependence!=localstack
RUN dotnet restore "KeeperData.Api.sln"
RUN dotnet build "src/KeeperData.Api/KeeperData.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:UseAppHost=false

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "src/KeeperData.Api" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true


# Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "KeeperData.Api.dll"]
