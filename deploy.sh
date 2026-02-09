#!/bin/bash

# Load environment variables (optional, or rely on .env file)
if [ -f .env ]; then
  echo "Loading from .env..."
  export $(grep -v '^#' .env | xargs)
fi

echo "Deploying EvenPlay Production..."

# Pull latest changes (assuming run from git repo)
# git pull origin main 

# Stop old containers
docker-compose -f docker-compose.prod.yml down

# Rebuild and start new containers
docker-compose -f docker-compose.prod.yml up -d --build

# Clean up unused images
docker system prune -f

echo "Deployment complete! 🚀"
