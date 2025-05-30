using System.ComponentModel.DataAnnotations;

namespace e_tour_api.Configuration
{
    public class AppSettings
    {
        [Required]
        public ConnectionStrings ConnectionStrings { get; set; } = new();
        [Required]
        public JwtSettings Jwt { get; set; } = new();
        [Required]
        public FrontendSettings Frontend { get; set; } = new();
        public FileStorageSettings FileStorage { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public CorsSettings Cors { get; set; } = new();
        public RateLimitingSettings RateLimiting { get; set; } = new();
        public EmailSettings Email { get; set; } = new();
        public EncryptionSettings Encryption { get; set; } = new();
    }

    public class ConnectionStrings
    {
        [Required]
        public string DefaultConnection { get; set; } = string.Empty;
    }

    public class JwtSettings
    {
        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;
        [Required]
        public string Issuer { get; set; } = string.Empty;
        [Required]
        public string Audience { get; set; } = string.Empty;
        [Range(1, 1440)]
        public int ExpiryMinutes { get; set; } = 15;
        [Range(1, 365)]
        public int RefreshExpiryDays { get; set; } = 7;
        [Range(1, 365)]
        public int KeyRotationDays { get; set; } = 30;
        [Range(32, 512)]
        public int MinimumKeyLength { get; set; } = 32;
        public bool RequireHttps { get; set; } = true;
        public bool ValidateIssuer { get; set; } = true;
        public bool ValidateAudience { get; set; } = true;
        public bool ValidateLifetime { get; set; } = true;
        public bool ValidateIssuerSigningKey { get; set; } = true;
    }

    public class FrontendSettings
    {
        [Required]
        [Url]
        public string Url { get; set; } = string.Empty;
    }

    public class FileStorageSettings
    {
        [Required]
        public string Path { get; set; } = "./uploads";
        [Range(1024, 104857600)] // 1KB to 100MB
        public long MaxSizeBytes { get; set; } = 10485760; // 10MB
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
    }

    public class SecuritySettings
    {
        public bool RequireHttps { get; set; } = true;
        [Range(8, 128)]
        public int PasswordMinLength { get; set; } = 8;
        [Range(1, 1440)]
        public int SessionTimeoutMinutes { get; set; } = 60;
    }

    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
        public string[] AllowedHeaders { get; set; } = new[] { "Content-Type", "Authorization" };
        public bool AllowCredentials { get; set; } = true;
    }

    public class RateLimitingSettings
    {
        [Range(1, 1000)]
        public int PermitLimit { get; set; } = 100;
        [Range(1, 60)]
        public int WindowMinutes { get; set; } = 1;
    }

    public class EmailSettings
    {
        [Required]
        public string SmtpServer { get; set; } = string.Empty;
        [Range(1, 65535)]
        public int SmtpPort { get; set; } = 587;
        [Required]
        [EmailAddress]
        public string SmtpUsername { get; set; } = string.Empty;
        [Required]
        public string SmtpPassword { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        public string FromEmail { get; set; } = string.Empty;
        [Required]
        [Url]
        public string BaseUrl { get; set; } = "http://localhost:3000";
    }

    public class EncryptionSettings
    {
        [Required]
        [MinLength(32)]
        public string Key { get; set; } = string.Empty;
    }
} 