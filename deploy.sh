#!/bin/bash

set -e

echo "Starting Docker containers..."
docker compose up --build -d

echo "✅ Deployment complete. Certificate created, container is up."