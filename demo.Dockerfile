# syntax=docker/dockerfile:1

### --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

# Copy everything
COPY . .

# Install global tools needed during build
RUN dotnet tool install -g dotnet-ef

# Install dependencies needed for build (like libgdiplus)
RUN apt-get update && apt-get install -y --no-install-recommends libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Publish the application
RUN dotnet publish API/API.csproj -c Release -o /app/out

### --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends libgdiplus \
    && rm -rf /var/lib/apt/lists/*

# Define build arguments (for environment variables)
ARG DB_USERNAME
ARG DB_PASSWORD
ARG ACCESS_KEY
ARG SECRET_KEY
ARG GOOGLE_API_KEY
ARG DEFAULT_PASSWORD
ARG SMTP_USERNAME
ARG SMTP_PASSWORD
ARG MONGO_INITDB_ROOT_USERNAME
ARG MONGO_INITDB_ROOT_PASSWORD

# Set environment variables
ENV ASPNETCORE_URLS=http://+:${CONTAINER_PORT:-5006}
ENV connectionString="Host=postgres_db;Port=5432;Username=${DB_USERNAME};Password=${DB_PASSWORD};Database=oryxdbdemo"
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
ENV CLIENT_BASE_URL="http://164.90.142.68:3006"
ENV MONGO_DB_CONNECTION_STRING="mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongodb:27017"
ENV Environment="demo"

# Copy published output
COPY --from=build /app/out .

EXPOSE 5006

ENTRYPOINT ["dotnet", "API.dll"]