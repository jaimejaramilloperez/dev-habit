![.NET](https://img.shields.io/badge/-.NET%209.0-blueviolet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white)
![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=fff)
![OpenAPI](https://img.shields.io/badge/-Swagger-85EA2D?style=flat&logo=swagger&logoColor=white)
![OpenAPI](https://img.shields.io/badge/-Scalar-0F0F0F?style=flat&logo=swagger&logoColor=white)

# DevHabit API

## Tabla de Contenidos

<ol>
  <li>
    <a href="#-resumen">Resumen</a>
  </li>
  <li>
    <a href="#-tecnologías-utilizadas">Tecnologías Utilizadas</a>
  </li>
  <li>
    <a href="#-características">Características</a>
  </li>
  <li>
    <a href="#-primeros-pasos">Primeros Pasos</a>
    <ul>
      <li><a href="#-requisitos-previos">Requisitos Previos</a></li>
      <li><a href="#-instalación">Instalación</a></li>
    </ul>
  </li>
  <li>
    <a href="#-configuración-del-entorno">Configuración del Entorno</a>
    <ul>
      <li><a href="#-desarrollo-en-local">Desarrollo en Local</a></li>
      <li><a href="#-docker-para-desarrollo">Docker para Desarrollo</a></li>
      <li><a href="#-docker-para-producción">Docker para Producción</a></li>
    </ul>
  </li>
  <li>
    <a href="#-pruebas">Pruebas</a>
  </li>
  <li>
    <a href="#-documentación-de-la-api">Documentación de la API</a>
  </li>
</ol>


## 🎯 Resumen

DevHabit API es un servicio web RESTful versionado, construido con .NET 9, que ayuda a los usuarios a hacer seguimiento de hábitos y rutinas personales. Ofrece autenticación segura basada en JWT, procesamiento de tareas en segundo plano, integración con GitHub y observabilidad estructurada mediante OpenTelemetry. El sistema utiliza PostgreSQL, Docker y sigue buenas prácticas en validación, pruebas y despliegue. Soporta desarrollo local, pipelines de CI/CD y entornos de producción en contenedores.

Este proyecto fue desarrollado siguiendo el curso [Pragmatic REST APIs](https://www.milanjovanovic.tech/pragmatic-rest-apis) de [Milan Jovanović](https://github.com/m-jovanovic), usando las últimas características de ASP.NET Core y buenas prácticas recomendadas.

## 🔧 Tecnologías Utilizadas

- [.NET 9](https://dotnet.microsoft.com/) – Framework moderno para construir APIs web escalables
- [PostgreSQL](https://www.postgresql.org/) – Base de datos relacional de código abierto
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) – ORM para acceso a datos
- [Docker](https://www.docker.com/) – Contenerización e infraestructura local
- [Swagger (Swashbuckle)](https://swagger.io/) – Documentación interactiva basada en OpenAPI
- [Scalar](https://scalar.com/) – Explorador interactivo de APIs (alternativo)
- [FluentValidation](https://docs.fluentvalidation.net/) – Framework de validación de modelos
- [Quartz.NET](https://www.quartz-scheduler.net/) – Tareas en segundo plano y programación
- [Polly (via Microsoft.Extensions.Http.Resilience)](https://www.pollydocs.org/) – Manejo de fallos y resiliencia
- [Refit](https://reactiveui.github.io/refit/) – Clientes REST tipados
- [CsvHelper](https://joshclose.github.io/CsvHelper/) – Utilidades para importar/exportar archivos CSV
- [OpenTelemetry](https://opentelemetry.io/) – Trazabilidad distribuida y observabilidad
- [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) – Modelo simplificado de aplicaciones distribuidas con soporte para OpenTelemetry
- [Seq](https://datalust.co/seq) – Agregación centralizada de logs (opcional)
- [Azure Monitor](https://learn.microsoft.com/en-us/azure/azure-monitor/) – Integración de telemetría (opcional)
- [xUnit](https://xunit.net/) – Framework para pruebas unitarias
- [NSubstitute](https://nsubstitute.github.io/) – Mocking amigable para .NET
- [Testcontainers](https://dotnet.testcontainers.org/) – Pruebas de integración con contenedores
- [WireMock.Net](https://wiremock.org/) – Simulación y mocking de HTTP
- [SonarAnalyzer](https://rules.sonarsource.com/csharp) – Análisis estático de código

## ✨ Características

#### 🧭 Diseño de la API
- Filtrado, ordenamiento, paginación y modelado de datos en colecciones de recursos
- Negociación de contenido y soporte para HATEOAS
- Versionado de la API con soporte para media types
- Clientes HTTP tipados mediante Refit
- Documentación interactiva de la API con Swagger y Scalar

#### ⚙️ Infraestructura e Integración
- PostgreSQL con EF Core y convenciones de nombres
- Tareas en segundo plano con Quartz
- Integración con GitHub para datos externos o automatización
- Importación de datos desde archivos (soporte para CSV)

#### 🔐 Seguridad
- Autenticación y autorización basada en JWT
- Configuración de políticas CORS
- Almacenamiento seguro de llaves de API de GitHub mediante encriptación

#### 📈 Observabilidad y Resiliencia
- Trazabilidad distribuida basada en OpenTelemetry
- Soporte para health checks
- Patrones de resiliencia HTTP mediante Polly (.NET Resilience)
- Azure Monitor (integración opcional)
- Logging con .NET Aspire o Seq

#### 🧪 Stack de Pruebas
- Pruebas unitarias con xUnit y NSubstitute
- Pruebas de integración con Testcontainers y PostgreSQL
- Mocking HTTP mediante WireMock.Net
- Cobertura de código con Coverlet

#### 📦 Despliegue y DevOps
- Soporte para desarrollo local con Docker Compose
- Versionado centralizado de paquetes con MSBuild
- Soporte para imágenes de producción en Docker

## 🚀 Primeros Pasos

### 📋 Requisitos Previos

Asegúrate de tener instalado el CLI de .NET en tu sistema. Puedes verificarlo ejecutando:

```bash
dotnet --version
```

Esto debería mostrar la versión instalada del CLI de .NET. Si no está instalado, descárgelo desde la [pagina oficial de .NET](https://dotnet.microsoft.com/download).

Para verificar qué versiones del SDK están instalados:

```bash
dotnet --list-sdks
```

> [!IMPORTANT]
> La versión mínima requerida del SDK de .NET es **9.0.0**

Adicionalmente, el proyecto utiliza Docker para ejecutar servicios de soporte (por ejemplo, PostgreSQL, pgAdmin, Seq). Necesitará:

- [**Docker**](https://www.docker.com/products/docker-desktop): Se recomienda instalar Docker Desktop.
- [**Docker  Compose**](https://docs.docker.com/compose/): Usualmente incluido con Docker Desktop.

Para comprobar que Docker está instalado y en funcionamiento:

```bash
docker --version
docker compose version
```

Si estos comandos fallan o arrojan errores, consulte la [guía de instalación de Docker](https://docs.docker.com/get-docker/).

---

### 📥 Instalación

Para comenzar, clona el repositorio y configura el entorno:

1. Clone el repositorio:

```bash
git clone https://github.com/jaimejaramilloperez/dev-habit.git
```

2. Navege al directorio del proyecto:

```bash
cd dev-habit
```

3. Genere y confíe en el certificado de desarrollo HTTPS:

```bash
dotnet dev-certs https -ep ./src/DevHabit.Api/aspnetapp.pfx -p Test1234!
dotnet dev-certs https --trust
```

4. Copie la plantilla de entorno y configúrelo:

```bash
cp .env.template .env
# Edite el archivo .env según sea necesario
```

Después de la instalación, estarás listo para ejecutar la aplicación localmente o usando Docker. Consulta las secciones de [Desarrollo en local](#-desarrollo-en-local) o [Docker](#-docker-para-desarrollo) para más detalles.

## 💻 Configuración del Entorno

Configura tu entorno para ejecutar la API de DevHabit ya sea localmente o con Docker, según tu flujo de trabajo.

> [!NOTE]
> Los valores de configuración mostrados (por ejemplo, contraseñas, puertos, claves, cadenas de conexión) se proporcionan solo con fines demostrativos. Puede modificarlos según sea necesario — especialmente para entornos de producción.

#### Nota sobre Observabilidad:

Este proyecto soporta trazabilidad distribuida y telemetría mediante el [Dashboard de .NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) o [Seq](https://datalust.co/seq).

Según la herramienta elegida, deberá definir las siguientes variables de entorno:

- `OTEL_EXPORTER_OTLP_ENDPOINT`
- `OTEL_EXPORTER_OTLP_PROTOCOL`

Para .NET Aspire:

```dotenv
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:18889
OTEL_EXPORTER_OTLP_PROTOCOL=grpc
```
Para Seq:

```dotenv
OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:5341/ingest/otlp
OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf
```

> [!TIP]
> Si la API se ejecuta dentro de un contenedor Docker, `localhost` hace referencia al propio contenedor. En ese caso, reemplace `localhost` con el nombre del servicio definido en la red de Docker (por ejemplo, `devhabit.seq` o `devhabit.aspire-dashboard`).

Puede alternar entre ambas opciones modificando el archivo `.env` o los archivos de entorno correspondientes a Docker.

---

### 🧑‍💻 Desarrollo en Local

Puede ejecutar la API localmente utilizando la CLI de .NET, mientras los servicios de soporte (PostgreSQL, pgAdmin, Seq) se ejecutan a través de Docker Compose.

1. Configure los secretos de usuario:

Los valores sensibles deben almacenarse de forma segura mediante [user secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-9.0&tabs=linux):

```json
{
  "ConnectionStrings:Database": "Server=localhost;Port=5432;Database=devhabit;Username=devhabit;Password=123456;",
  "Jwt:Key": "HTycXOjdDRfrtNYzQQbkx2L7ncCEe2989cWH6yrTFdSPRmFFe4K9qmbnjHJBRGHfaeRKvDEWzaS",
  "Encryption:Key": "Ubf/RatKuzJ4p8Fc9nr9LKZFV5L8CjIZZCqcFlYZeEo="
}
```

2. Actualice `appsettings.Development.json`

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

3. Configure las variables de entorno (archivo `.env`):

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

4. Inicie los servicios de Docker:

```bash
docker compose up -d
```

5. Ejecute la API:

```bash
dotnet run --project src/DevHabit.Api
# o con HTTPS
dotnet run --launch-profile https --project src/DevHabit.Api
```

---

### 🐳 Docker para Desarrollo

Este modo ejecuta tanto la aplicación como los servicios en contenedores Docker utilizando una imagen de desarrollo.

1. Cree o ajuste el archivo `.env.docker-debug`:

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

2. Inicie los contenedores:

```bash
docker compose -f ./docker-compose.debug.yml --env-file .env.docker-debug up -d
```

#### 🐞 Depuración en el contenedor

> [!IMPORTANT]
> La depuración dentro de contenedores requiere el depurador `vsdbg`. Si aún no está instalado, consulte la [guía oficial de configuración](https://github.com/microsoft/MIEngine/wiki/Offroad-Debugging-of-.NET-Core-on-Linux---OSX-from-Visual-Studio) para instrucciones sobre cómo instalarlo manualmente.

Si está utilizando Visual Studio Code, puede depurar la aplicación que se ejecuta dentro del contenedor de dos formas:

1. **Containers: .NET Launch**

Esta opción compila y ejecuta el contenedor en modo depuración y lanza automáticamente el depurador.

Asegúrese de tener un archivo `.env.docker-debug-image` con la siguiente configuración:

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

Esta opción más sencilla permite adjuntar el depurador a un contenedor en ejecución, sin necesidad de configuración adicional más allá de iniciar los contenedores como se describe en el paso 2 de la sección [docker para desarrollo](#-docker-para-desarrollo).

> [!WARNING]
> Esta funcionalidad aún se encuentra en versión preliminar (preview) y podría presentar un rendimiento general más lento.

---

### 📦 Docker para Producción

Para compilar y ejecutar la API en modo producción utilizando una imagen Docker mínima:

1. Cree o ajuste el archivo `.env.docker-prod`:

```dotenv
DOTNET_USE_POLLING_FILE_WATCHER="1"
ASPNETCORE_ENVIRONMENT="Production"
ASPNETCORE_HTTP_PORTS=5000

ConnectionStrings__Database=Server=devhabit.postgres;Port=5432;Database=devhabit;Username=devhabit;Password=123456;

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
APPLICATIONINSIGHTS_CONNECTION_STRING=<your-azure-application-insights-connection-string>
```

2. Compile la imagen:

```bash
docker buildx build \
  --platform linux/amd64 \
  -f src/DevHabit.Api/Dockerfile \
  --target prod \
  -t devhabit.api:latest .
```

2. Inicie los contenedores:

```bash
docker compose up -d
```

3. Ejecute el contenedor:

```bash
docker container run \
  -d \
  -p 5000:5000 \
  --env-file .env.docker-prod \
  --network dev-habit_default \
  --name devhabit.api \
  devhabit.api:latest
```

## 🧪 Pruebas

Este proyecto incluye pruebas unitarias, de integración y funcionales.

> [!NOTE]
> Las pruebas se encuentran en el directorio `test/`.

#### Tecnologías utilizadas:

- **xUnit**: Framework de pruebas.
- **NSubstitute**: Mocking.
- **Testcontainers**: Pruebas de integración con instancias efímeras de PostgreSQL.
- **WireMock.Net**: Simulación de APIs externas.
- **coverlet**: Cobertura de código.
- **Microsoft.AspNetCore.Mvc.Testing**: Pruebas funcionales de extremo a extremo.

#### Ejecutar todas las pruebas

```bash
dotnet test
```

#### Cobertura de código

Para generar un informe de cobertura de código en formato HTML, es necesario instalar [dotnet reportgenerator tool](https://reportgenerator.io/).

Puede instalarla globalmente con el siguiente comando:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

Existe un script auxiliar ubicado en `./scripts/coverage.sh` que automatiza el proceso.

Antes de ejecutar el script, asegúrese de otorgar permisos de ejecución:

```bash
chmod +x ./scripts/coverage.sh
```

Luego ejecútelo con argumentos opcionales:

```bash
./scripts/coverage.sh [output_dir] [verbosity]
```

- **output_dir**: Directorio donde se guardará el informe (por defecto: `coverage`)
- **verbosity**: Nivel de detalle del ReportGenerator (por defecto: `Error`)

Una vez ejecutado, el informe HTML estará disponible en:

```bash
<output_dir>/index.html
```

> [!TIP]
> Si utiliza Windows, puede usar la versión PowerShell del script: `.\scripts\coverage.ps1`

## 📘 Documentación de la API

DevHabit API ofrece documentación interactiva a través de Swagger y Scalar, con soporte para endpoints versionados y autenticación JWT.

Una vez la API esté en ejecución:

- **OpenAPI spec (JSON)**: `https://localhost:5001/swagger/1.0/swagger.json`
- **Swagger UI**: `https://localhost:5001/swagger`
- **Scalar UI**: `https://localhost:5001/scalar`

> [!NOTE]
> Reemplace `5001` con el puerto HTTPS correspondiente si utiliza uno distinto.
