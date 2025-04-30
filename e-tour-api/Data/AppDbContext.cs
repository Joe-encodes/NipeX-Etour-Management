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
                    FilePath = "path/to/test-document-1.pdf",
                    Status = "Pending",
                    Signature = null,
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Static value
                },
                new Document
                {
                    Id = 2,
                    FileName = "Test Document 2",
                    FilePath = "path/to/test-document-2.pdf",
                    Status = "Signed",
                    Signature = "Approver Signature",
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) // Static value
                }
            );

            modelBuilder.Entity<Document>()
                .HasOne(d => d.User)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UserId);
        }
    }
}