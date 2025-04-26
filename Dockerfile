# syntax=docker/dockerfile:1

# --------
# Stage 1: Build
# --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env

WORKDIR /app

# Copy everything
COPY . . 

# Install any tools needed for building
RUN dotnet tool install -g dotnet-ef

# Install any Linux libraries needed
RUN apt-get update && apt-get install -y --allow-unauthenticated libgdiplus

# Set PATH so global tools work
ENV PATH=$PATH:/root/.dotnet/tools

# Restore dependencies
RUN dotnet restore

# Build the project (Release mode)
RUN dotnet publish API/API.csproj -c Release -o /app/out

# --------
# Stage 2: Runtime
# --------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

# Install runtime dependencies (again, if needed)
RUN apt-get update && apt-get install -y --allow-unauthenticated libgdiplus

# Define build arguments (for environment variables)
ARG DB_USERNAME
ARG DB_PASSWORD
ARG ACCESS_KEY
ARG SECRET_KEY
ARG DEFAULT_PASSWORD
ARG SMTP_USERNAME
ARG SMTP_PASSWORD
ARG MONGO_INITDB_ROOT_USERNAME
ARG MONGO_INITDB_ROOT_PASSWORD

# Set environment variables
ENV ASPNETCORE_URLS=http://+:${CONTAINER_PORT:-5001}
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
ENV MONGO_INITDB_ROOT_USERNAME="${MONGO_INITDB_ROOT_USERNAME}"
ENV MONGO_INITDB_ROOT_PASSWORD="${MONGO_INITDB_ROOT_PASSWORD}"
ENV MONGO_DB_CONNECTION_STRING="mongodb://${MONGO_INITDB_ROOT_USERNAME}:${MONGO_INITDB_ROOT_PASSWORD}@mongodb:27017"
ENV Environment="dev"

# Copy the built output from build-env
COPY --from=build-env /app/out .

EXPOSE ${CONTAINER_PORT:-5001}

# Run the app
ENTRYPOINT ["dotnet", "API.dll"]