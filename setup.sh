#!/usr/bin/env bash
set -euo pipefail

# Colors for output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Function to print status messages
print_status() {
    echo -e "${YELLOW}$1${NC}"
}

print_success() {
    echo -e "${GREEN}$1${NC}"
}

print_error() {
    echo -e "${RED}$1${NC}"
}

# Function to check if a command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to check system requirements
check_requirements() {
    print_status "🔍 Checking system requirements..."

    # Check Node.js
    if ! command_exists node; then
        print_error "⚠️ Node.js is required. Please install from https://nodejs.org/"
        exit 1
    fi
    NODE_VERSION=$(node --version)
    print_success "✅ Node.js: $NODE_VERSION"

    # Check .NET SDK
    if ! command_exists dotnet; then
        print_error "⚠️ .NET SDK is required. Please install from https://dotnet.microsoft.com/download"
        exit 1
    fi
    DOTNET_VERSION=$(dotnet --version)
    print_success "✅ .NET SDK: $DOTNET_VERSION"

    # Check Docker
    if ! command_exists docker; then
        print_error "⚠️ Docker is required. Please install from https://docs.docker.com/get-docker/"
        exit 1
    fi
    DOCKER_VERSION=$(docker --version)
    print_success "✅ Docker: $DOCKER_VERSION"

    # Check Docker Compose
    if ! command_exists docker-compose; then
        print_error "⚠️ Docker Compose is required. Please install from https://docs.docker.com/compose/install/"
        exit 1
    fi
    COMPOSE_VERSION=$(docker-compose --version)
    print_success "✅ Docker Compose: $COMPOSE_VERSION"
}

# Function to create .env file
create_env_file() {
    if [ ! -f .env ]; then
        print_status "Creating .env file..."
        cat > .env << EOL
# Database Configuration
DB_CONNECTION_STRING=Host=db;Database=etour;Username=postgres;Password=postgres
DB_HOST=db
DB_NAME=etour
DB_USER=postgres
DB_PASSWORD=postgres

# JWT Configuration
JWT_KEY=YourJWTSecretKeyHere
JWT_ISSUER=http://localhost:5372
JWT_AUDIENCE=http://localhost:5372

# Frontend Configuration
REACT_APP_API_URL=http://localhost:5372
REACT_APP_ENV=development
EOL
        print_success ".env file created successfully"
    else
        print_success ".env file already exists"
    fi
}

# Function to wait for database
wait_for_database() {
    print_status "Waiting for database to be ready..."
    until docker-compose exec db pg_isready -U postgres -d etour >/dev/null 2>&1; do
        print_status "Database is unavailable - sleeping"
        sleep 2
    done
    print_success "Database is ready!"
}

# Function to verify database setup
verify_database() {
    print_status "Verifying database setup..."
    
    # Check if tables exist
    if ! docker-compose exec db psql -U postgres -d etour -c "\dt" | grep -q "Documents"; then
        print_error "Error: Database tables not created"
        return 1
    fi
    
    # Check if migrations are applied
    if ! docker-compose exec db psql -U postgres -d etour -c "SELECT COUNT(*) FROM \"__EFMigrationsHistory\";" | grep -q "9"; then
        print_error "Error: Migrations not applied correctly"
        return 1
    fi
    
    print_success "Database setup verified successfully"
    return 0
}

# Main setup process
main() {
    print_status "🚀 Starting NipeX E-Tour Management Setup..."

    # Check system requirements
    check_requirements

    # Create .env file
    create_env_file

    # Stop any running containers and remove volumes
    print_status "Cleaning up existing containers..."
    docker-compose down -v

    # Start database
    print_status "Starting database..."
    docker-compose up -d db

    # Wait for database
    wait_for_database

    # Start backend
    print_status "Starting backend..."
    docker-compose up -d backend

    # Wait for backend
    print_status "Waiting for backend to be ready..."
    until curl -s http://localhost:5372/health >/dev/null 2>&1; do
        print_status "Backend is unavailable - sleeping"
        sleep 2
    done
    print_success "Backend is ready!"

    # Start frontend
    print_status "Starting frontend..."
    docker-compose up -d frontend

    # Verify database setup
    if ! verify_database; then
        print_error "Database verification failed"
        exit 1
    fi

    # Print success message and access information
    print_success "\n🎉 Setup completed successfully!"
    print_success "Frontend: http://localhost:3000"
    print_success "Backend API: http://localhost:5372"
    print_success "Swagger UI: http://localhost:5372/swagger"
    print_status "\nDefault admin credentials:"
    echo "Username: admin@nipex.com"
    echo "Password: YourStrongPassword123!"
}

# Run the main function
main 