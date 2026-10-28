# syntax=docker/dockerfile:1
# --------
# Stage 1: Build
# --------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Install dependencies first
RUN dotnet tool install -g dotnet-ef --version 9.0.3 && \
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
COPY tests/APP.Tests/*.csproj ./tests/APP.Tests/
COPY tests/ProcedureSpikeWorker/*.csproj ./tests/ProcedureSpikeWorker/
COPY TOOLS/FormulaMigration/*.csproj ./TOOLS/FormulaMigration/

# Restore dependencies (this layer will be cached if project files don't change)
RUN dotnet restore

# Copy the rest of the source code
COPY API/ ./API/
COPY APP/ ./APP/
COPY DOMAIN/ ./DOMAIN/
COPY INFRASTRUCTURE/ ./INFRASTRUCTURE/
COPY SHARED/ ./SHARED/
COPY tests/APP.Tests/ ./tests/APP.Tests/

# Build and publish in one step
# Build one project at a time; retained migration snapshots need substantial RAM.
RUN dotnet publish API/API.csproj -c Release -o /app/out --no-restore \
    --disable-build-servers -m:1 -p:UseSharedCompilation=false

# One-shot migration job. Compose supplies the same connectionString as the API.
# The design-time factory constructs only the DbContext; it does not start the API or seed data.
FROM build-env AS migration
ENTRYPOINT ["dotnet", "ef", "database", "update", "--no-build", "--configuration", "Release", "--project", "INFRASTRUCTURE/INFRASTRUCTURE.csproj", "--startup-project", "API/API.csproj"]

# ---------
# Stage 2: Runtime
# ---------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ARG RELEASE_SHA=unversioned
LABEL org.opencontainers.image.revision=$RELEASE_SHA

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
