using System;
using System.Text.Json.Serialization;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Tests.Tests
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Signature { get; set; }
        public string? SignedFilePath { get; set; }
        public int UserId { get; set; }
        public UserProfileDto? User { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
} 