# syntax=docker/dockerfile:1
# --------
# Stage 1: Build
# --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Install dependencies first
RUN dotnet tool install -g dotnet-ef --version 8.0.10 && \
    apt-get update && \
    apt-get install -y --no-install-recommends libgdiplus && \
    rm -rf /var/lib/apt/lists/*

ENV PATH=$PATH:/root/.dotnet/tools

# Copy only solution and project files first (for better caching)
COPY *.sln ./
COPY API/*.csproj ./API/
COPY APP/*.csproj ./APP/
COPY DOMAIN/*.csproj ./DOMAIN/
COPY INFRASTRUCTURE/*.csproj ./INFRASTRUCTURE/
COPY SHARED/*.csproj ./SHARED/

# Restore dependencies (this layer will be cached if project files don't change)
RUN dotnet restore

# Copy the rest of the source code
COPY API/ ./API/
COPY APP/ ./APP/
COPY DOMAIN/ ./DOMAIN/
COPY INFRASTRUCTURE/ ./INFRASTRUCTURE/
COPY SHARED/ ./SHARED/

# Build and publish in one step
RUN dotnet publish API/API.csproj -c Release -o /app/out --no-restore

# ---------
# Stage 2: Runtime
# ---------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN apt-get update && \
    apt-get install -y --no-install-recommends libgdiplus && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5001 \
    redisConnectionString="redis:6379,abortConnect=false" \
    MINIO_ENDPOINT="minio" \
    MINIO_PORT=9000 \
    REDIS_HOST="redis" \
    REDIS_PORT=6379 \
    CLIENT_BASE_URL="oryx-next" \
    Environment="dev"

# Copy the published output
COPY --from=build-env /app/out .

EXPOSE 5001

ENTRYPOINT ["dotnet", "API.dll"]