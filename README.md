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
docker container inspect --format='{{json .State.Health}}' api
```

### Generate certificate for development

```sh
dotnet dev-certs https -ep ./src/DevHabit.Api/aspnetapp.pfx -p Test1234!
```
