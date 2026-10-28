# Base dotnet image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Add curl to template.
# CDP PLATFORM HEALTHCHECK REQUIREMENT
RUN apt update && \
    apt install curl -y && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Build stage image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG TARGETARCH
ENV BUILD_CONFIGURATION=${BUILD_CONFIGURATION}
WORKDIR /src

COPY ["src/KeeperData.Api/KeeperData.Api.csproj", "KeeperData.Api/"]
COPY ["src/KeeperData.Infrastructure/KeeperData.Infrastructure.csproj", "KeeperData.Infrastructure/"]
COPY ["src/KeeperData.Application/KeeperData.Application.csproj", "KeeperData.Application/"]
COPY ["src/KeeperData.Core/KeeperData.Core.csproj", "KeeperData.Core/"]

RUN set -eu; \
    case "${TARGETARCH}" in amd64|x64) dotnet_arch=x64 ;; arm64) dotnet_arch=arm64 ;; *) echo "Unsupported architecture: ${TARGETARCH}" >&2; exit 1 ;; esac; \
    dotnet restore "KeeperData.Api/KeeperData.Api.csproj" -r linux-${dotnet_arch} -v n; \
    dotnet restore "KeeperData.Infrastructure/KeeperData.Infrastructure.csproj" -r linux-${dotnet_arch} -v n; \
    dotnet restore "KeeperData.Application/KeeperData.Application.csproj" -r linux-${dotnet_arch} -v n; \
    dotnet restore "KeeperData.Core/KeeperData.Core.csproj" -r linux-${dotnet_arch} -v n

COPY ["src/", "."]

FROM build AS publish
ARG TARGETARCH
WORKDIR "/src/KeeperData.Api"
RUN set -eu; \
    case "${TARGETARCH}" in amd64|x64) dotnet_arch=x64 ;; arm64) dotnet_arch=arm64 ;; *) echo "Unsupported architecture: ${TARGETARCH}" >&2; exit 1 ;; esac; \
    dotnet publish "KeeperData.Api.csproj" -v n -c ${BUILD_CONFIGURATION} -o /app/publish -r linux-${dotnet_arch} --no-restore /p:UseAppHost=false

ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Final production image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8085
ENTRYPOINT ["dotnet", "KeeperData.Api.dll"]
