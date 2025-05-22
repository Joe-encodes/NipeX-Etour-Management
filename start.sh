#!/bin/bash

set -e  # Exit on any error

# Function to check if database is ready
wait_for_db() {
    echo "Waiting for database to be ready..."
    until pg_isready -h ${DB_HOST:-db} -p ${DB_PORT:-5432} -U ${DB_USER:-postgres}; do
        echo "Database is unavailable - sleeping"
        sleep 2
    done
    echo "Database is up - executing command"
}

# Function to run migrations
run_migrations() {
    echo "Running database migrations..."
    echo "Installing dotnet-ef tool..."
    dotnet tool install --global dotnet-ef
    export PATH="$PATH:/root/.dotnet/tools"
    echo "PATH updated: $PATH"
    echo "Running migrations..."
    cd /app/e-tour-api
    /root/.dotnet/tools/dotnet-ef database update --verbose
}

# Wait for database
wait_for_db

# Run migrations
run_migrations

# Start the backend with debug logging
echo "Starting backend..."
set -x
cd /app/backend
dotnet e-tour-api.dll --urls "http://0.0.0.0:8080" > /app/backend.log 2>&1
set +x 