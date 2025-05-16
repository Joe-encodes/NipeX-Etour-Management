// AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System;

namespace e_tour_api.Data

{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Document> Documents { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>().HasData(
                new Document
                {
                    Id = 1,
                    FileName = "Test Document 1",
                    FilePath = "../wwwroot/uploads/3fdb0d6a-b94e-437f-8feb-95ead990f86d.pdf",
                    Status = "Pending",
                    Signature = null,
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Static value
                },
                new Document
                {
                    Id = 2,
                    FileName = "Test Document 2",
                    FilePath = "../wwwroot/uploads/1a0649bc-6b35-4cb1-83df-2232a5793c3c.PDF",
                    Status = "Signed",
                    Signature = "Approver Signature",
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Static value
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User {
                    Id = 1,
                    Username     = "testuser",
                    PasswordHash = "$2a$11$xSvbaWPoLyCJzJ178mGP/.sMmecXPG.eeEjGRLEj1P0zI.F5iAFzW", // HashGenerator.HashPassword("testpassword"),
                    Email        = "testuser@example.com",
                    Role         = "User"
                },
                new User {
                    Id = 2,
                    Username     = "approver",
                    PasswordHash = "$2a$11$6MU9z5yN3i7QgeTZoqF8Bu7JussC0kxASimKesjyGAKxyAisA9YMq", // HashGenerator.HashPassword("approverpassword"),
                    Email        = "approver@example.com",
                    Role         = "Approver"
                },
                new User {
                    Id = 3,
                    Username     = "admin",
                    PasswordHash = "$2a$11$DhTruhJZQ0ss2N3PPAiXo.dcDeIohWgUGCCxXPCpCrFD9dhXdBkqy", // HashGenerator.HashPassword("adminpassword"),
                    Email        = "admin@example.com",
                    Role         = "Admin"
                }
            );

            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId);
        }
    }
}