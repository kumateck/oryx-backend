#!/bin/bash

set -e

echo "Starting Docker containers..."
docker compose up --build -d

echo "✅ Deployment complete. container is up."