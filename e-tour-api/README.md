# E-Tour API

## Configuration Management

### Configuration Strategy

The application follows a clear configuration strategy to avoid confusion and maintain security:

1. **Development Environment**:
   - Use `.env` file for all values (including sensitive data)
   - `appsettings.Development.json` contains only non-sensitive defaults
   - Never commit `.env` file (it's in .gitignore)
   - Example: `cp .env.example .env`

2. **Production Environment**:
   - Use environment variables for ALL values
   - `appsettings.Production.json` contains only structure (empty values)
   - No sensitive data in any committed files
   - Set variables in your hosting platform (Azure, AWS, etc.)

3. **Configuration Priority**:
   ```
   Environment Variables (highest)
   ↓
   .env file (development only)
   ↓
   appsettings.{Environment}.json
   ↓
   appsettings.json (lowest)
   ```

### Required Environment Variables

These MUST be set in production via environment variables, or in development via `.env`:

```bash
# Database
DATABASE_CONNECTION_STRING=your-connection-string

# JWT
JWT_KEY=your-secure-key
JWT_ISSUER=your-domain
JWT_AUDIENCE=your-domain

# Frontend
FRONTEND_URL=your-frontend-url
CORS_ALLOWED_ORIGINS=your-frontend-url
```

### Optional Environment Variables

These can use defaults from `appsettings.json` if not set:

```bash
# JWT Settings
JWT_EXPIRY_MINUTES=15
JWT_REFRESH_EXPIRY_DAYS=7
JWT_KEY_ROTATION_DAYS=30
JWT_MINIMUM_KEY_LENGTH=32
JWT_REQUIRE_HTTPS=true
JWT_VALIDATE_ISSUER=true
JWT_VALIDATE_AUDIENCE=true
JWT_VALIDATE_LIFETIME=true
JWT_VALIDATE_ISSUER_SIGNING_KEY=true

# Other Settings
RATE_LIMIT_PERMIT=100
RATE_LIMIT_WINDOW_MINUTES=1
FILE_STORAGE_PATH=./uploads
FILE_MAX_SIZE_BYTES=10485760
FILE_ALLOWED_EXTENSIONS=.pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png
REQUIRE_HTTPS=true
PASSWORD_MIN_LENGTH=8
SESSION_TIMEOUT_MINUTES=60
```

### Why This Approach?

1. **Security**:
   - No sensitive data in source control
   - Clear separation between development and production
   - Environment-specific values stay in their environment

2. **Clarity**:
   - Single source of truth for each environment
   - No confusion about which values to use
   - Clear documentation of required variables

3. **Maintenance**:
   - Easy to update values without changing code
   - No need to modify appsettings files for different environments
   - Simple to add new configuration options

## Configuration

### Environment Variables

The application uses environment variables for configuration. In development, these can be set in a `.env` file. In production, they should be set in your hosting environment.

Required environment variables:

- `DATABASE_CONNECTION_STRING`: PostgreSQL connection string
- `JWT_KEY`: Secret key for JWT token signing (minimum 32 characters)
- `JWT_ISSUER`: The issuer of the JWT tokens (your API domain)
- `JWT_AUDIENCE`: The audience of the JWT tokens (your API domain)
- `FRONTEND_URL`: The URL of your frontend application
- `CORS_ALLOWED_ORIGINS`: Comma-separated list of allowed CORS origins

Optional environment variables (will use defaults from appsettings.json if not set):

- `JWT_EXPIRY_MINUTES`: JWT token expiry in minutes (default: 15)
- `JWT_REFRESH_EXPIRY_DAYS`: Refresh token expiry in days (default: 7)
- `JWT_KEY_ROTATION_DAYS`: Days until JWT key rotation (default: 30)
- `JWT_MINIMUM_KEY_LENGTH`: Minimum length for JWT key (default: 32)
- `JWT_REQUIRE_HTTPS`: Whether to require HTTPS for JWT (default: true)
- `JWT_VALIDATE_ISSUER`: Whether to validate JWT issuer (default: true)
- `JWT_VALIDATE_AUDIENCE`: Whether to validate JWT audience (default: true)
- `JWT_VALIDATE_LIFETIME`: Whether to validate JWT lifetime (default: true)
- `JWT_VALIDATE_ISSUER_SIGNING_KEY`: Whether to validate JWT signing key (default: true)
- `RATE_LIMIT_PERMIT`: Number of requests allowed per window (default: 100)
- `RATE_LIMIT_WINDOW_MINUTES`: Rate limit window in minutes (default: 1)
- `FILE_STORAGE_PATH`: Path for file uploads (default: ./uploads)
- `FILE_MAX_SIZE_BYTES`: Maximum file size in bytes (default: 10485760)
- `FILE_ALLOWED_EXTENSIONS`: Comma-separated list of allowed file extensions
- `REQUIRE_HTTPS`: Whether to require HTTPS (default: true)
- `PASSWORD_MIN_LENGTH`: Minimum password length (default: 8)
- `SESSION_TIMEOUT_MINUTES`: Session timeout in minutes (default: 60)

### JWT Security

The application implements several security measures for JWT tokens:

1. **Key Requirements**:
   - Minimum key length of 32 characters
   - Key rotation every 30 days (configurable)
   - HTTPS required for token transmission

2. **Token Validation**:
   - Issuer validation
   - Audience validation
   - Lifetime validation
   - Signing key validation

3. **Token Expiry**:
   - Access tokens expire after 15 minutes
   - Refresh tokens expire after 7 days

4. **Best Practices**:
   - Use strong, randomly generated keys in production
   - Rotate keys regularly
   - Store keys securely (e.g., Azure Key Vault, AWS KMS)
   - Use HTTPS for all token transmission
   - Validate all token claims

### Development vs Production

- Development: Uses `appsettings.Development.json` with development-friendly defaults
- Production: Uses `appsettings.Production.json` with secure defaults, requires environment variables

## Development

### Prerequisites
- .NET 8 SDK
- PostgreSQL
- Node.js (for frontend)

### Running Locally
1. Set up your `.env` file
2. Run the database migrations
3. Start the API:
   ```bash
   dotnet run
   ```

## Production Deployment
1. Set all required environment variables in your hosting platform
2. Deploy the application
3. Run database migrations

## Running the Application

1. Set up your environment variables (see above)
2. Run database migrations:
   ```bash
   dotnet ef database update
   ```
3. Start the application:
   ```bash
   dotnet run
   ```

## API Documentation

API documentation is available at `/swagger` when running in development mode.

// ... rest of README ... 