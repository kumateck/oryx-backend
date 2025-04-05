# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /app
COPY . .

RUN dotnet tool install -g dotnet-ef

RUN apt-get update && apt-get install -y --allow-unauthenticated libgdiplus

# Define build arguments
ARG DB_USERNAME
ARG DB_PASSWORD
ARG ACCESS_KEY
ARG SECRET_KEY
ARG DEFAULT_PASSWORD
ARG SMTP_USERNAME
ARG SMTP_PASSWORD

# Set environment variables
ENV DOTNET_USE_POLLING_FILE_WATCHER=1
ENV PATH=$PATH:/root/.dotnet/tools
ENV connectionString="Host=postgres_db;Port=5432;Username=${DB_USERNAME};Password=${DB_PASSWORD};Database=oryxdb"
ENV redisConnectionString="redis:6379,abortConnect=false"
ENV MINIO_ENDPOINT="minio"
ENV MINIO_ACCESS_KEY="${ACCESS_KEY}"
ENV MINIO_SECRET_KEY="${SECRET_KEY}"
ENV MINIO_PORT=9000
ENV REDIS_HOST="redis"
ENV REDIS_PORT=6379 
ENV DEFAULT_USER_PASSWORD="${DEFAULT_PASSWORD}"
ENV SMTP_USERNAME="${SMTP_USERNAME}"
ENV SMTP_PASSWORD="${SMTP_PASSWORD}"
ENV CLIENT_BASE_URL="http://164.90.142.68:3005"
ENV MONGO_DB_CONNECTION_STRING="mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongo:27018"

RUN dotnet restore Oryx.sln

ENTRYPOINT ["dotnet", "watch", "run", "--urls=http://+:5001", "--project", "API/API.csproj"]