#!/bin/bash

echo "Logging in to Docker Hub"
docker login -u "$DOCKER_USERNAME" -p "$DOCKER_PASSWORD"

echo "Pulling the latest Docker image: $DOCKER_IMAGE_NAME:latest"
docker pull "$DOCKER_IMAGE_NAME:latest"

echo "Stopping and removing the existing container if it exists"
# shellcheck disable=SC2046
# shellcheck disable=SC2046
# shellcheck disable=SC2126
if [ $(docker ps -a | grep "$CONTAINER_NAME" | wc -l) -gt 0 ]; then
  docker stop "$CONTAINER_NAME"
  docker rm "$CONTAINER_NAME"
fi

echo "Pruning unused Docker images"
docker image prune -a -f

echo "Running the new Docker container on the external 'sail' network"
docker run -d --net sail -p "$HOST_PORT:$CONTAINER_PORT" --name "$CONTAINER_NAME" "$DOCKER_IMAGE_NAME:latest"
