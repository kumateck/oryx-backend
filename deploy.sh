#!/bin/bash

set -e

echo "Starting Docker containers..."
docker compose build --progress=plain

docker compose up -d

echo "✅ Deployment complete. Containers are up."
echo "📋 Viewing logs (Ctrl+C to exit, containers will keep running)..."
docker compose logs -f