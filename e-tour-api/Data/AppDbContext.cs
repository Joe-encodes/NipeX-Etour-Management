// AppDbContext.cs
using e_tour_api.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System;

namespace e_tour_api.Data
{
    /// <summary>
    /// Database context for the e-tour API application.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Gets or sets the Users DbSet.
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets or sets the Documents DbSet.
        /// </summary>
        public DbSet<Document> Documents { get; set; }

        /// <summary>
        /// Gets or sets the RefreshTokens DbSet.
        /// </summary>
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        /// <summary>
        /// Gets or sets the Messages DbSet.
        /// </summary>
        public DbSet<Message> Messages { get; set; } = null!;

        /// <summary>
        /// Gets or sets the DocumentApprovals DbSet.
        /// </summary>
        public DbSet<DocumentApproval> DocumentApprovals { get; set; }

        /// <summary>
        /// Initializes a new instance of the AppDbContext class.
        /// </summary>
        /// <param name="options">The options to be used by a DbContext.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Configures the model and seeds initial data.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20);
                entity.Property(e => e.ProfilePicture).HasMaxLength(500);
                entity.Property(e => e.Bio).HasMaxLength(500);
            });

            // Message configuration
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.ReceiverId);
                entity.HasIndex(e => e.CreatedAt);

                entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
                entity.Property(e => e.EncryptedContent).IsRequired();

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.SentMessages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();

                entity.HasOne(e => e.Receiver)
                    .WithMany(u => u.ReceivedMessages)
                    .HasForeignKey(e => e.ReceiverId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

            // Document configuration
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.UploadedAt);

                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.FilePath).HasMaxLength(500);
                entity.Property(e => e.Signature).HasMaxLength(100);
                entity.Property(e => e.SignedFilePath).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Documents)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId);

                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.ExpiryDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();
            });

            // DocumentApproval configuration
            modelBuilder.Entity<DocumentApproval>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.DocumentId);
                entity.HasIndex(e => e.ApproverId);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.AssignedAt);

                entity.Property(e => e.Comments).HasMaxLength(1000);

                entity.HasOne(e => e.Document)
                    .WithMany()
                    .HasForeignKey(e => e.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                entity.HasOne(e => e.Approver)
                    .WithMany(u => u.AssignedApprovals)
                    .HasForeignKey(e => e.ApproverId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .IsRequired();
            });

            // Configure Message-User relationships
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed initial data
            modelBuilder.Entity<Document>().HasData(
                new Document
                {
                    Id = 1,
                    FileName = "Test Document 1",
                    FilePath = "../wwwroot/uploads/3fdb0d6a-b94e-437f-8feb-95ead990f86d.pdf",
                    Status = DocumentStatus.Pending,
                    Signature = null,
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Document
                {
                    Id = 2,
                    FileName = "Test Document 2",
                    FilePath = "../wwwroot/uploads/1a0649bc-6b35-4cb1-83df-2232a5793c3c.PDF",
                    Status = DocumentStatus.Signed,
                    Signature = "Approver Signature",
                    UserId = 1,
                    UploadedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );

            modelBuilder.Entity<User>().HasData(
                new User {
                    Id = 1,
                    Username = "testuser",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("testpassword"),
                    Email = "testuser@example.com",
                    Role = "User"
                },
                new User {
                    Id = 2,
                    Username = "approver",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("approverpassword"),
                    Email = "approver@example.com",
                    Role = "Approver"
                },
                new User {
                    Id = 3,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminpassword"),
                    Email = "admin@example.com",
                    Role = "Admin"
                }
            );

            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Token).IsRequired();
                entity.Property(e => e.ExpiryDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
