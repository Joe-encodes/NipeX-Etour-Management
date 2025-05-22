# Development stage
FROM mcr.microsoft.com/dotnet/sdk:8.0.409 AS development
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["e-tour-api.csproj", "./"]
RUN dotnet restore --no-cache

# Copy everything else
COPY . .
EXPOSE 80
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0.409 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["e-tour-api.csproj", "./"]
RUN dotnet restore --no-cache

# Copy everything else and build
COPY . .
RUN dotnet publish "e-tour-api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Use the official ASP.NET runtime image for runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Expose port 80
EXPOSE 80

# Set environment variables for DB connection and JWT key (to be overridden at runtime)
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true

ENTRYPOINT ["dotnet", "e-tour-api.dll"]
