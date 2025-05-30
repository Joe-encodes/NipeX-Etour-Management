// AppDbContextFactory.cs

using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace e_tour_api.Data
{
    public class AppDbContextFactory 
        : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            // 1. Build the same configuration your app uses:
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())       // e-tour-api folder
                .AddJsonFile("appsettings.json", optional: false)   // core settings
                .AddJsonFile($"appsettings.{environment}.json", optional: true)  // env‑specific overrides
                .AddEnvironmentVariables()                          // final override
                .Build();

            // 2. Read the connection string from configuration
            var conn = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(conn))
                throw new InvalidOperationException(
                    "Could not find a connection string named 'DefaultConnection'");

            // 3. Configure the DbContextOptions for PostgreSQL
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseNpgsql(conn, opts =>
                opts.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
