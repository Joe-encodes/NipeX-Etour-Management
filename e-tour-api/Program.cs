// Program.cs

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using e_tour_api.Data;
using e_tour_api.Models;
using e_tour_api.Utilities; // Added for HashGenerator
using Microsoft.OpenApi.Models; // For Swagger configuration
using e_tour_api.Services; // For IPdfStampService and PdfStampService
using System.Reflection; // Needed for Assembly
using DotNetEnv; // Added for .env file support
using e_tour_api.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using e_tour_api.Validators;
using Serilog;
using Serilog.Events;
using e_tour_api.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File("logs/etour-.log", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting web application");
            
            var builder = WebApplication.CreateBuilder(args);

            // Load .env file (development only)
            if (builder.Environment.IsDevelopment())
            {
                Env.Load();
            }

            // Configure strongly-typed settings
            var appSettings = builder.Configuration.Get<AppSettings>() ?? new AppSettings();
            
            // Override with environment variables if present
            // This ensures environment variables take precedence over appsettings.json
            appSettings.ConnectionStrings.DefaultConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                ?? appSettings.ConnectionStrings.DefaultConnection;
            
            appSettings.Jwt.Key = Environment.GetEnvironmentVariable("JWT_KEY") 
                ?? appSettings.Jwt.Key;
            appSettings.Jwt.Issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                ?? appSettings.Jwt.Issuer;
            appSettings.Jwt.Audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                ?? appSettings.Jwt.Audience;
            appSettings.Jwt.ExpiryMinutes = int.Parse(Environment.GetEnvironmentVariable("JWT_EXPIRY_MINUTES") 
                ?? appSettings.Jwt.ExpiryMinutes.ToString());
            appSettings.Jwt.RefreshExpiryDays = int.Parse(Environment.GetEnvironmentVariable("JWT_REFRESH_EXPIRY_DAYS") 
                ?? appSettings.Jwt.RefreshExpiryDays.ToString());
            
            appSettings.Frontend.Url = Environment.GetEnvironmentVariable("FRONTEND_URL") 
                ?? appSettings.Frontend.Url;

            // Email settings
            appSettings.Email.SmtpServer = Environment.GetEnvironmentVariable("SMTP_SERVER") 
                ?? appSettings.Email.SmtpServer;
            appSettings.Email.SmtpPort = int.Parse(Environment.GetEnvironmentVariable("SMTP_PORT") 
                ?? appSettings.Email.SmtpPort.ToString());
            appSettings.Email.SmtpUsername = Environment.GetEnvironmentVariable("SMTP_USERNAME") 
                ?? appSettings.Email.SmtpUsername;
            appSettings.Email.SmtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") 
                ?? appSettings.Email.SmtpPassword;
            appSettings.Email.FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") 
                ?? appSettings.Email.FromEmail;
            appSettings.Email.BaseUrl = Environment.GetEnvironmentVariable("EMAIL_BASE_URL") 
                ?? appSettings.Email.BaseUrl;

            // File storage settings
            appSettings.FileStorage.Path = Environment.GetEnvironmentVariable("FILE_STORAGE_PATH") 
                ?? appSettings.FileStorage.Path;
            appSettings.FileStorage.MaxSizeBytes = long.Parse(Environment.GetEnvironmentVariable("FILE_MAX_SIZE_BYTES") 
                ?? appSettings.FileStorage.MaxSizeBytes.ToString());
            var allowedExtensions = Environment.GetEnvironmentVariable("FILE_ALLOWED_EXTENSIONS");
            if (!string.IsNullOrEmpty(allowedExtensions))
            {
                appSettings.FileStorage.AllowedExtensions = allowedExtensions.Split(',');
            }

            // Security settings
            appSettings.Security.RequireHttps = bool.Parse(Environment.GetEnvironmentVariable("REQUIRE_HTTPS") 
                ?? appSettings.Security.RequireHttps.ToString());
            appSettings.Security.PasswordMinLength = int.Parse(Environment.GetEnvironmentVariable("PASSWORD_MIN_LENGTH") 
                ?? appSettings.Security.PasswordMinLength.ToString());
            appSettings.Security.SessionTimeoutMinutes = int.Parse(Environment.GetEnvironmentVariable("SESSION_TIMEOUT_MINUTES") 
                ?? appSettings.Security.SessionTimeoutMinutes.ToString());

            // Rate limiting settings
            appSettings.RateLimiting.PermitLimit = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_PERMIT") 
                ?? appSettings.RateLimiting.PermitLimit.ToString());
            appSettings.RateLimiting.WindowMinutes = int.Parse(Environment.GetEnvironmentVariable("RATE_LIMIT_WINDOW_MINUTES") 
                ?? appSettings.RateLimiting.WindowMinutes.ToString());

            // Encryption settings
            appSettings.Encryption.Key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY") 
                ?? appSettings.Encryption.Key;

            // Validate required settings
            if (builder.Environment.IsProduction())
            {
                ValidateProductionSettings(appSettings);
            }

            // Register settings as singleton
            builder.Services.AddSingleton(appSettings);

            // Configure Entity Framework Core with PostgreSQL
            if (string.IsNullOrEmpty(appSettings.ConnectionStrings.DefaultConnection))
            {
                throw new InvalidOperationException("Database connection string is not configured.");
            }
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(appSettings.ConnectionStrings.DefaultConnection));

            // Configure JWT Authentication
            if (string.IsNullOrEmpty(appSettings.Jwt.Key))
            {
                throw new InvalidOperationException("JWT Key is not configured.");
            }

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = appSettings.Jwt.Issuer,
                    ValidAudience = appSettings.Jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(appSettings.Jwt.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        // First try to get the token from the Authorization header
                        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                        
                        // If not found in header, try to get from cookie
                        if (string.IsNullOrEmpty(token) && context.Request.Cookies.ContainsKey("X-Access-Token"))
                        {
                            token = context.Request.Cookies["X-Access-Token"];
                        }

                        context.Token = token;
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // Add Swagger/OpenAPI support
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo 
                { 
                    Title = "E-Tour API", 
                    Version = "v1",
                    Description = "API for E-Tour Management System",
                    Contact = new OpenApiContact
                    {
                        Name = "E-Tour Support",
                        Email = "support@etour.com"
                    }
                });

                // Add JWT Authentication support in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments if available
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("Approver", policy =>
                    policy.RequireRole("Approver"));
            });

            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IPdfStampService, PdfStampService>();
            builder.Services.AddScoped<IMessageService, MessageService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<CleanupService>();

            // Add memory cache
            builder.Services.AddMemoryCache();

            // Configure rate limiting
            builder.Services.AddRateLimiter(options =>
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));
                
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            // Configure CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    var allowedOrigins = new[]
                    {
                        "https://localhost:3000",
                        "http://localhost:3000",
                        appSettings.Frontend.Url
                    }.Where(url => !string.IsNullOrEmpty(url)).ToArray();

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
            });

            // Add controllers and API endpoints
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            // Add EmailService
            builder.Services.AddScoped<IEmailService, EmailService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Tour API V1");
                });
            }

            // Add middleware in the correct order
            app.UseMiddleware<ErrorHandlingMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseMiddleware<RequestValidationMiddleware>();
            app.UseMiddleware<SecurityHeadersMiddleware>();
            
            app.UseCors("FrontendPolicy");
            
            // Ensure these are in the correct order
            app.UseRouting();
            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            // Map controllers after authentication middleware
            app.MapControllers();

            // Seed initial users at startup
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var databaseProvider = context.Database.ProviderName;

                if (!string.IsNullOrEmpty(databaseProvider) &&
                    databaseProvider != "Microsoft.EntityFrameworkCore.InMemory")
                {
                    context.Database.Migrate();
                }

                if (!context.Users.Any())
                {
                    context.Users.AddRange(
                        new User
                        {
                            Id = 1,
                            Username = "admin@nipex.com",
                            PasswordHash = HashGenerator.HashPassword("YourStrongPassword123!"),
                            Email = "admin@nipex.com",
                            Role = "Admin"
                        }
                    );
                    context.SaveChanges();
                }
            }

            app.UseRateLimiter();
            app.UseStaticFiles();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ValidateProductionSettings(AppSettings settings)
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(settings.ConnectionStrings.DefaultConnection))
            errors.Add("DefaultConnection is required in production");
        
        if (string.IsNullOrEmpty(settings.Jwt.Key))
            errors.Add("JWT_KEY is required in production");
        
        if (string.IsNullOrEmpty(settings.Jwt.Issuer))
            errors.Add("JWT_ISSUER is required in production");
        
        if (string.IsNullOrEmpty(settings.Jwt.Audience))
            errors.Add("JWT_AUDIENCE is required in production");
        
        if (string.IsNullOrEmpty(settings.Frontend.Url))
            errors.Add("FRONTEND_URL is required in production");

        if (errors.Any())
        {
            throw new InvalidOperationException(
                "Missing required environment variables in production:\n" + 
                string.Join("\n", errors));
        }
    }
}
