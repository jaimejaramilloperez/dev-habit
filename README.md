![.NET](https://img.shields.io/badge/-.NET%209.0-blueviolet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=fff)
![OpenAPI](https://img.shields.io/badge/-Swagger-85EA2D?style=flat&logo=swagger&logoColor=white)
![OpenAPI](https://img.shields.io/badge/-Scalar-0F0F0F?style=flat&logo=swagger&logoColor=white)

# DevHabit API

> [!TIP]
> 📘 This project is also available in [Spanish](./README-ES.md).

## Table of Contents

<ol>
  <li>
    <a href="#-overview">Overview</a>
  </li>
  <li>
    <a href="#-technologies">Technologies</a>
  </li>
  <li>
    <a href="#-features">Features</a>
  </li>
  <li>
    <a href="#-getting-started">Getting Started</a>
    <ul>
      <li><a href="#-prerequisites">Prerequisites</a></li>
      <li><a href="#-installation">Installation</a></li>
    </ul>
  </li>
  <li>
    <a href="#-environment-setup">Environment Setup</a>
    <ul>
      <li><a href="#-local-development">Local Development</a></li>
      <li><a href="#-docker-for-development">Docker for Development</a></li>
      <li><a href="#-docker-for-production">Docker for Production</a></li>
    </ul>
  </li>
  <li>
    <a href="#-testing">Testing</a>
  </li>
  <li>
    <a href="#-api-documentation">API Documentation</a>
  </li>
</ol>

## 🎯 Overview

DevHabit API is a versioned RESTful web service built with .NET 9 that helps users track personal habits and routines. It provides secure JWT-based authentication, background job processing, GitHub integration, and structured observability through OpenTelemetry. The system use PostgreSQL, Docker, and best practices in validation, testing, and deployment. It supports local development, CI/CD pipelines, and containerized production environments.

This project was built following [Milan Jovanović](https://github.com/m-jovanovic)'s course [Pragmatic REST APIs](https://www.milanjovanovic.tech/pragmatic-rest-apis), using the latest ASP.NET Core features and best practices.

## 🔧 Technologies

- [.NET 9](https://dotnet.microsoft.com/) – Modern framework for building scalable web APIs
- [PostgreSQL](https://www.postgresql.org/) – Open-source relational database
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) – ORM for data access
- [Docker](https://www.docker.com/) – Containerization and local infrastructure
- [Swagger (Swashbuckle)](https://swagger.io/) – OpenAPI interactive documentation
- [Scalar](https://scalar.com/) – Alternative interactive API explorer
- [FluentValidation](https://docs.fluentvalidation.net/) – Model validation framework
- [Quartz.NET](https://www.quartz-scheduler.net/) – Background jobs and scheduling
- [Polly (via Microsoft.Extensions.Http.Resilience)](https://www.pollydocs.org/) – Fault-handling and resiliency
- [Refit](https://reactiveui.github.io/refit/) – Typed REST API clients
- [CsvHelper](https://joshclose.github.io/CsvHelper/) – CSV import/export utilities
- [OpenTelemetry](https://opentelemetry.io/) – Distributed tracing and observability
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) – Simplified distributed application model with OpenTelemetry support
- [Seq](https://datalust.co/seq) – Optional centralized log aggregation
- [Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/) – Telemetry integration (optional)
- [xUnit](https://xunit.net/) – Unit testing framework
- [NSubstitute](https://nsubstitute.github.io/) – Friendly mocking for .NET
- [Testcontainers](https://dotnet.testcontainers.org/) – Integration testing with containers
- [WireMock.Net](https://wiremock.org/) – HTTP stubbing and mocking
- [SonarAnalyzer](https://rules.sonarsource.com/csharp) – Static code analysis

## ✨ Features

#### 🧭 API Design
- Filtering, sorting, pagination, and data shaping on resource collections
- Content negotiation and HATEOAS support
- API versioning with media type support
- Typed HTTP clients via Refit
- Interactive API docs with Swagger and Scalar

#### ⚙️ Infrastructure & Integration
- PostgreSQL with EF Core and naming conventions
- Background jobs with Quartz
- GitHub integration for external data or automation
- File-based data import (CSV support)

#### 🔐 Security
- JWT-based authentication and authorization
- CORS policy configuration
- secure storage of GitHub api keys via encryption

#### 📈 Observability & Resilience
- OpenTelemetry-based distributed tracing
- Health checks support
- HTTP resiliency patterns via Polly (.NET Resilience)
- Azure Monitor (optional integration)
- Logging with .Net Aspire or Seq

#### 🧪 Testing Stack
- Unit testing with xUnit and NSubstitute
- Integration testing with Testcontainers and PostgreSQL
- HTTP mocking via WireMock.Net
- Code coverage with Coverlet

#### 📦 Deployment & DevOps
- Local development support with Docker Compose
- Centralized package versioning with MSBuild
- Dockerized production image support

## 🚀 Getting Started

### 📋 Prerequisites

Make sure you have .NET CLI installed on your system. You can check if it's available by running:

```bash
dotnet --version
```

This should print the installed version of the .NET CLI. If it's not installed, download it from the [official .NET site](https://dotnet.microsoft.com/download).

To verify which SDK versions are installed:

```bash
dotnet --list-sdks
```

> [!IMPORTANT]
> The minimum .NET SDK version required is **9.0.0**

Additionally, the project uses Docker for running supporting services (e.g., PostgreSQL, pgAdmin, Seq). You’ll need:

- [**Docker**](https://www.docker.com/products/docker-desktop): Recommended to install Docker Desktop.
- [**Docker  Compose**](https://docs.docker.com/compose/): Typically included with Docker Desktop.

To check that Docker is installed and running:

```bash
docker --version
docker compose version
```

If these commands fail or return errors, refer to the [Docker installation guide](https://docs.docker.com/get-docker/).

---

### 📥 Installation

To get started, clone the repository and set up the environment configuration:

1. Clone the repository:

```bash
git clone https://github.com/jaimejaramilloperez/dev-habit.git
```

2. Navigate to the project directory:

```bash
cd dev-habit
```

3. Generate and trust the HTTPS development certificate:

```bash
dotnet dev-certs https -ep ./src/DevHabit.Api/aspnetapp.pfx -p Test1234!
dotnet dev-certs https --trust
```

4. Copy the environment template and configure it:

```bash
cp .env.template .env
# Edit the .env file as needed
```

After installation, you're ready to run the app either locally or using Docker. See the [Local Development](#-local-development) or [Docker](#-docker-for-development) sections for details.

## 💻 Environment SetUp

Set up your environment to run the DevHabit API either locally or with Docker, depending on your workflow.

> [!NOTE]
> The configuration values shown (e.g., passwords, ports, keys, connection strings) are provided for demonstration purposes only. You are free to modify them as needed — especially for production environments.

#### Observability Notice:

This project supports distributed tracing and telemetry via either [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/) or [Seq](https://datalust.co/seq).

Depending on which one you choose, you must configure the following environment variables:

- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL`

For .NET Aspire:

```dotenv
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```
For Seq:

```dotenv
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5341/ingest/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
```

> [!TIP]
> If the API runs inside a Docker container, `localhost` refers to the container itself. In that case, replace `localhost` with the service name defined in your Docker network (e.g., `devhabit.seq` or `devhabit.aspire-dashboard`).

You can switch between them by updating your `.env` or Docker environment files.

---

### 🧑‍💻 Local Development

You can run the API locally using the .NET CLI and supporting services (PostgreSQL, pgAdmin, Seq) via Docker Compose.

1. Configure user secrets:

Sensitive values should be stored securely using [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=linux):

```json
{
  "ConnectionStrings:Database": "Server=localhost;Port=5432;Database=devhabit;Username=devhabit;Password=123456;",
  "Jwt:Key": "HTycXOjdDRfrtNYzQQbkx2L7ncCEe2989cWH6yrTFdSPRmFFe4K9qmbnjHJBRGHfaeRKvDEWzaS",
  "Encryption:Key": "Ubf/RatKuzJ4p8Fc9nr9LKZFV5L8CjIZZCqcFlYZeEo="
}
```

2. Update `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "Database": "your-database-connection-string-here"
  },
  "Jwt": {
    "Key": "your-secret-key-here-that-should-also-be-fairly-long",
    "Issuer": "dev-habit.api",
    "Audience": "dev-habit.app",
    "ExpirationInMinutes": 30,
    "RefreshTokenExpirationInDays": 7
  },
  "Encryption": {
    "Key": "your-secret-key-here-that-should-also-be-exactly-32-bytes-or-44-characters-in-base64-long"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000"
    ]
  },
  "Jobs": {
    "ScanIntervalInMinutes": 50
  },
  "GitHub": {
    "BaseUrl": "https://api.github.com"
  },
  "OTEL_EXPORTER_OTLP_ENDPOINT": "http://localhost:18889",
  "OTEL_EXPORTER_OTLP_PROTOCOL": "grpc",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

3. Configure environment variables (`.env` file):

```dotenv
# postgresql
POSTGRES_PORT="5432"
POSTGRES_DB="devhabit"
POSTGRES_USER="spartan"
POSTGRES_PASSWORD="123456"

# pgadmin
PGADMIN_EMAIL="user@mail.com"
PGADMIN_PASSWORD="123456"

# seq
SEQ_PASSWORD="12345678"
```

4. Start docker services:

```bash
docker compose up -d
```

5. Run the API:

```bash
dotnet run --project src/DevHabit.Api
# or with HTTPS
dotnet run --launch-profile https --project src/DevHabit.Api
```

---

### 🐳 Docker for Development

This mode runs both the application and services in Docker containers using a development image.

1. Create or adjust `.env.docker-debug`:

```dotenv
# docker
DOCKER_REGISTRY=""
DOCKER_IMAGE_TAG="dev"
DOCKER_BUILD_TARGET="dev"
DOCKER_ASPNETCORE_BUILD_CONFIGURATION="Debug"

# app
ENVIRONMENT="Development"
HTTP_PORT="5000"
HTTPS_PORT="5001"
CERTIFICATE_PATH="/https/aspnetapp.pfx"
CERTIFICATE_PASSWORD="Test1234!"
ENCRYPTION_KEY="Ubf/RatKuzJ4p8Fc9nr9LKZFV5L8CjIZZCqcFlYZeEo="
OTEL_EXPORTER_OTLP_ENDPOINT="http://devhabit.aspire-dashboard:18889"
OTEL_EXPORTER_OTLP_PROTOCOL="grpc"

# postgresql
POSTGRES_SERVER="devhabit.postgres"
POSTGRES_PORT="5432"
POSTGRES_DB="devhabit"
POSTGRES_USER="devhabit"
POSTGRES_PASSWORD="123456"

# pgadmin
PGADMIN_EMAIL="user@mail.com"
PGADMIN_PASSWORD="123456"

# seq
SEQ_PASSWORD="12345678"
```

2. Start the containers:

```bash
docker compose -f ./docker-compose.debug.yml --env-file .env.docker-debug up -d
```

#### 🐞 Debugging in container

> [!IMPORTANT]
> Debugging inside containers requires the `vsdbg` debugger. If it’s not already installed, refer to the [official setup guide](https://github.com/microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio) for instructions on how to install it manually.

If you're using Visual Studio Code, you can debug the application running inside a container in two ways:

1. **Containers: .NET Launch**

This option builds and runs the container in debug mode and launches the debugger automatically.

Make sure you have a `.env.docker-debug-image` file with the following configuration:

```dotenv
ASPNETCORE_HTTP_PORTS=5000
ASPNETCORE_HTTPS_PORTS=5001
ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=Test1234!
ConnectionStrings__Database=Server=devhabit.postgres;Port=5432;Database=devhabit;Username=devhabit;Password=123456;
OTEL_EXPORTER_OTLP_ENDPOINT=http://devhabit.aspire-dashboard:18889
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```

2. **Containers .NET Attach (Preview)**

This simpler option attaches the debugger to a running container — no extra configuration required beyond starting the containers as shown above in the step 2 of [docker for development](#-docker-for-development).

> [!WARNING]
> This feature is still in preview and may exhibit overall slower performance.

---

### 📦 Docker for Production

To build and run the API in production mode using a minimal Docker image:

1. Create or adjust `.env.docker-prod`:

```dotenv
DOTNET_USE_POLLING_FILE_WATCHER="1"
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_HTTP_PORTS="5000"

ConnectionStrings__Database="Server=devhabit.postgres;Port=5432;Database=devhabit;Username=devhabit;Password=123456;"

Jwt__Key="your-secret-key-here-that-should-also-be-fairly-long"
Jwt__Issuer="dev-habit.api"
Jwt__Audience="dev-habit.app"
Jwt__ExpirationInMinutes=30
Jwt__RefreshTokenExpirationInDays=7

Encryption__Key="Ubf/RatKuzJ4p8Fc9nr9LKZFV5L8CjIZZCqcFlYZeEo="
Cors__AllowedOrigins__0=""
Jobs__ScanIntervalInMinutes=50
GitHub__BaseUrl="https://api.github.com"

# Required
APPLICATIONINSIGHTS_CONNECTION_STRING="<your-azure-application-insights-connection-string>"
```

2. Build the image:

```bash
docker buildx build \
  --platform linux/amd64 \
  -f src/DevHabit.Api/Dockerfile \
  --target prod \
  -t devhabit.api:latest .
```

2. Start the containers:

```bash
docker compose up -d
```

3. Run the container:

```bash
docker container run \
  -d \
  -p 5000:5000 \
  --env-file .env.docker-prod \
  --network dev-habit_default \
  --name devhabit.api \
  devhabit.api:latest
```

## 🧪 Testing

This project includes unit, integration and functional testing.

> [!NOTE]
> Tests are located in the `test/` directory.

#### Testing Technologies:

- **xUnit**: Test framework.
- **NSubstitute**: Mocking.
- **Testcontainers**: Integration testing with ephemeral PostgreSQL instances.
- **WireMock.Net**: Mock external APIs.
- **coverlet**: Code coverage.
- **Microsoft.AspNetCore.Mvc.Testing**: End-to-end and functional tests.

#### Running all tests

```bash
dotnet test
```

#### Code coverage

To generate a code coverage report with HTML output, you’ll need the [dotnet reportgenerator tool](https://reportgenerator.io/).
You can install it globally using:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

A helper script is available at `./scripts/coverage.sh` to automate the process.

Before running the script, make sure it has execution permissions:

```bash
chmod +x ./scripts/coverage.sh
```

Then run it with optional arguments:

```bash
./scripts/coverage.sh [output_dir] [verbosity]
```

- **output_dir**: Directory where the report will be saved (default: `coverage`)
- **verbosity**: Verbosity level for ReportGenerator (default: `Error`)

Once executed, the HTML report will be available at:

```bash
<output_dir>/index.html
```

> [!TIP]
> If you're on Windows, you can use the PowerShell version of the script: `.\scripts\coverage.ps1`

## 📘 Api Documentation

DevHabit API provides interactive documentation via Swagger and Scalar, with support for versioned endpoints and JWT authentication.

Once the API is running:

- **OpenAPI spec (JSON)**: `https://localhost:5001/swagger/1.0/swagger.json`
- **Swagger UI**: `https://localhost:5001/swagger`
- **Scalar UI**: `https://localhost:5001/scalar`

> [!NOTE]
> Replace `5001` with your actual HTTPS port if different.
