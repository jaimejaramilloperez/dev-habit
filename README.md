### Build the docker image

```sh
docker buildx build --platform linux/amd64 -t api:latest -f src/DevHabit.Api/Dockerfile .
```

### Run the image in a container

```sh
docker container run -d -p 5000:8080 --name api api:latest

docker container run -d -p 80:80 -p 443:443 --env-file .env api:latest
```

### Check health

```sh
docker container inspect --format='{{json .State.Health}}' devhabit.api
```

### Generate certificate for development

```sh
dotnet dev-certs https -ep ./src/DevHabit.Api/aspnetapp.pfx -p Test1234!
```

### Create a database migration

```sh
dotnet dotnet-ef migrations add MigrationName -p src/DevHabit.Api -o Migrations/Application
```

## Env dev

```sh
export ConnectionStrings__Database="Server=localhost;Port=5432;Database=devhabit;Username=spartan;Password=123456;" &&
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:18889" &&
export OTEL_EXPORTER_OTLP_PROTOCOL="grpc" &&
source ~/.bashrc
```
