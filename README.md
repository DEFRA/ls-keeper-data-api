# Ls Keeper Data Api

Core delivery C# ASP.NET API providing RESTful data access for the Land Services Keeper Data Bridge.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
  - [Local Development Setup](#local-development-setup)
  - [Running the Application](#running-the-application)
  - [MongoDB Setup](#mongodb-setup)
  - [Inspect MongoDB](#inspect-mongodb)
- [Testing](#testing)
- [Development](#development)
  - [Building](#building)
  - [Code Quality](#code-quality)
  - [Contributing](#contributing)
  - [API Documentation](#api-documentation)
- [Deployment](#deployment)
  - [CDP Environments](#cdp-environments)
- [Architecture](#architecture)
- [Licence](#licence)

## Overview

This project provides an API that:
- Integrates with MongoDB for data persistence
- Leverages AWS services (S3, SQS) via LocalStack for local development
- Triggers daily jobs to scan for data updates
- Provides REST API endpoints for data import and querying
- Includes comprehensive unit, component, and integration tests

**Technology Stack:**
- .NET 10
- ASP.NET Core
- MongoDB
- Redis
- AWS (LocalStack for local development)
- Docker & Docker Compose

## Prerequisites

- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose** - [Download](https://www.docker.com/products/docker-desktop)
- **Git** - [Download](https://git-scm.com/)
- **MongoDB CLI tools** (optional) - [mongosh](https://www.mongodb.com/docs/mongodb-shell/install/)
- **MongoDB Compass** (optional tool for data visualisation) - [Download](https://www.mongodb.com/products/tools/compass)

## Project Structure

```
├── docs/
│   ├── api-specs/                             # api documentation yaml files
│   └── data-maps/                             
├── src/
│   ├── KeeperData.Api/                        # Main ASP.NET Core web application
│   ├── KeeperData.Api.Worker/                 # Background worker service
│   ├── KeeperData.Application/                # Application/use case logic
│   ├── KeeperData.Core/                       # Core domain models and entities
│   └── KeeperData.Infrastructure/             # Data access and external integrations
├── tests/
│   ├── KeeperData.Api.Tests.Component/        # Component tests (covering 2 or more layers of code)
│   ├── KeeperData.Api.Tests.Integration/      # Integration tests (running tests against temporary containers including the API and a Mongo instance)
│   ├── KeeperData.Api.Worker.Tests.Unit/
│   ├── KeeperData.Application.Tests.Unit/
│   ├── KeeperData.Core.Tests.Unit/
│   ├── KeeperData.Infrastructure.Tests.Unit/
│   ├── KeeperData.Tests.Common/
│   └── tools/
│       ├── CsvToJsonConverter.Tests/
│       └── TsvToJsonConverter.Tests/
├── tools/
│   ├── CsvToJsonConverter/
│   └── TsvToJsonConverter/
├── docker-compose.yml                         # Docker Compose configuration
└── README.md                                  # This file
```

## Getting Started

### Local Development Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/DEFRA/ls-keeper-data-api.git
   cd ls-keeper-data-api
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

### Running the Application

3. **Start the local development environment:**
   ```bash
   docker-compose -f docker-compose.yml -f docker-compose.override.yml up --build
   ```

   For MacOS use

   `docker-compose.override.mac.intel.yml` or `docker-compose.override.mac.arm.yml`

   This starts:
   - MongoDB
   - Redis
   - LocalStack (S3, SQS)
   - This service
     - root: `http://localhost:5555`
     - endpoints - see [Api Documentation](#api-documentation)


4. **Verify services are running:**
   ```bash
   docker compose ps
   ```
   Or open Docker Desktop dashboard and inspect the containers tab.


A more extensive setup is available in [github.com/DEFRA/cdp-local-environment](https://github.com/DEFRA/cdp-local-environment)

### MongoDB Setup

#### Option 1: Docker (Recommended)

MongoDB is included in the Docker Compose setup:

```bash
docker compose up -d mongodb
```

#### Option 2: Local Installation

- Install [MongoDB Community Server](https://www.mongodb.com/docs/manual/tutorial/#installation)
- Start MongoDB:
  ```bash
  # macOS / Linux
  sudo mongod --dbpath ~/mongodb-cdp

  # Windows (run as Administrator)
  mongod --dbpath C:\mongodb-cdp
  ```


#### Option 3: CDP Environments

In CDP environments, MongoDB is pre-configured and credentials are exposed via environment variables.

### Inspect MongoDB

* To view databases and collections via mongodb shell:

  ```bash
  # Connect to MongoDB shell
  mongosh

  # Common commands
  show databases
  use ls-keeper-data-api
  show collections
  db.collection_name.find().pretty()
  ```

MongoDB Compass can also be useful to connect to and visualise the database.

You can use the CDP Terminal to access MongoDB in remote environments.


## Testing

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Project
```bash
dotnet test tests/KeeperData.Application.Tests.Unit
dotnet test tests/KeeperData.Core.Tests.Unit
dotnet test tests/KeeperData.Infrastructure.Tests.Unit
#...etc...
```

### Unit Tests

The unit test projects test individual units of single classes or small groups of related classes and use mocking.

### Component Tests

The component test projects use the ASP.NET Core in-memory `TestServer` to simulate end-to-end interactions.  Data access is mocked.

### Integration Tests

Tests in the `KeeperData.Api.Tests.Integration` project run using docker containers to create a temporary local simulated environment. These containers run under the same names and on the same ports as the local docker instance you will use when running the environment locally, so you will need to ensure the environment is not running before triggering these tests. To do so you can use `docker compose down`.

## Development

### Building

```bash
# Build solution
dotnet build

# Build specific project
dotnet build src/KeeperData.Application

# Build with specific configuration
dotnet build -c Release
```

### Code Quality

#### Code Standards: ####
- Follow C# coding conventions
- Write unit tests for new features
- In the CI build, warnings are set to errors and will fail the build.
  Before committing run:
  ```bash
  dotnet build ./KeeperData.Api.sln --configuration Release --no-restore -warnaserror  
  dotnet format ./KeeperData.Api.sln --verbosity diagnostic
  ```
- Ensure all tests pass
- Update documentation as needed

#### SonarCloud

Example SonarCloud configuration are available in the GitHub Action workflows.

#### Dependabot

We have added an example dependabot configuration file to the repository. You can enable it by renaming
the [.github/example.dependabot.yml](.github/example.dependabot.yml) to `.github/dependabot.yml`

### Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Make your changes and commit: `git commit -am 'Add feature'`
3. Push to the branch: `git push origin feature/your-feature`
4. Submit a Pull Request

### Api Documentation

* healthcheck endpoint: `/health`
* countries endpoint: `/api/countries`
* sites endpoint: `/api/sites`
* parties endpoint: `/api/parties`

For detailed API documentation, refer to controller files in `src/KeeperData.Api/Controllers/`.

## Deployment

### CDP Environments

For deployment to CDP environments:

1. Ensure all required environment variables are configured
2. MongoDB credentials are automatically injected
3. Follow [CDP deployment documentation](https://github.com/DEFRA/cdp-local-environment)

## Architecture

### Layered Architecture

```
Controllers (API endpoints)
    ↓
Application Layer (Use cases, validation)
    ↓
Domain Layer (Business logic, entities)
    ↓
Infrastructure Layer (Data access, external services)
```

### Key Components

- **Controllers** - HTTP request handlers
- **Services** - Application business logic
- **Repository Pattern** - Data access abstraction
- **Middleware** - Cross-cutting concerns (authentication, exception handling)
- **Authentication** - API Key and No-Auth handlers

## Licence

This project is licensed under the **Open Government Licence v3.0 (OGL)**

The Open Government Licence was developed by the Controller of Her Majesty's Stationery Office (HMSO) to enable information providers in the public sector to license the use and re-use of their information under a common open licence.

It is designed to encourage use and re-use of information freely and flexibly, with only a few conditions.

See the [Licence page](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/) for full details.
