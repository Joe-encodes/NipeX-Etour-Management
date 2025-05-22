# Build frontend
FROM node:16 AS frontend-build
WORKDIR /app
COPY e-tour-frontend/package*.json ./
RUN npm install
COPY e-tour-frontend/ ./
RUN npm run build

# Build backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend-build
WORKDIR /app
COPY e-tour-api/*.csproj ./
RUN dotnet restore
COPY e-tour-api/ ./
RUN dotnet publish -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Install nginx and postgres client
RUN apt-get update && \
    apt-get install -y nginx postgresql-client procps && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Copy frontend build
COPY --from=frontend-build /app/build /usr/share/nginx/html
COPY e-tour-frontend/nginx.conf /etc/nginx/conf.d/default.conf

# Copy backend build and source
COPY --from=backend-build /app/publish /app/backend
COPY e-tour-api /app/e-tour-api

# Configure nginx
RUN echo "daemon off;" >> /etc/nginx/nginx.conf

# Expose port 80
EXPOSE 80

# Start both nginx and the backend
COPY start.sh /app/start.sh
RUN chmod +x /app/start.sh
CMD ["/app/start.sh"] 