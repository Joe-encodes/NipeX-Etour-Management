// Program.cs

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using e_tour_api.Data;
using e_tour_api.Utilities; // Added for HashGenerator
using Microsoft.OpenApi.Models; // For Swagger configuration
using e_tour_api.Services; // For IPdfStampService and PdfStampService
using System.Reflection; // Needed for Assembly


public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
            });

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", builder =>
            {
                builder.WithOrigins(
                    Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:80",
                    "http://localhost:3000"
                )
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        // Configure Entity Framework Core with PostgreSQL
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Database connection string is not configured.");
        }
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Configure JWT Authentication
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
            ?? builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
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
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
            };
        });
        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";

        // Add Swagger
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "E-Tour API", Version = "v1" });

            var xmlFile = "e-tour-api.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true); // Only do this if file exists

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT: Bearer {token}",
                Name = "Authorization",
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
        });


        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Approver", policy =>
                policy.RequireRole("Approver"));
        });

        builder.Services.AddScoped<IDocumentService, DocumentService>(); // Register the DocumentService
        builder.Services.AddScoped<IPdfStampService, PdfStampService>(); // Register the PdfStampService
        builder.Services.AddScoped<CleanupService>(); // Register the CleanupService
        builder.Services.AddScoped<IUserRepository, UserRepository>(); // Register the UserRepository

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
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseCors("AllowFrontend");
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();

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

        app.MapControllers();
        app.Run();
    }
}
