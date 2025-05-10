@echo off

echo "Building docker images..."
docker compose build

echo "Running engine and 3 reference bots"
docker compose up -d
docker compose logs --follow engine