using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using e_tour_api.Data;
using e_tour_api.Models;
using e_tour_api.Models.DTOs;
using User = e_tour_api.Models.User;  // Use Models.User instead of Data.User

namespace e_tour_api.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserService> _logger;
        private readonly IWebHostEnvironment _env;

        public UserService(AppDbContext context, ILogger<UserService> logger, IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        public async Task<ServiceResult<UserProfileDto>> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<UserProfileDto>.Error("User not found", 404);
            return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
        }

        public async Task<ServiceResult<UserProfileDto>> UpdateUserProfileAsync(int userId, string? phoneNumber, string? bio)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<UserProfileDto>.Error("User not found", 404);
            user.PhoneNumber = phoneNumber;
            user.Bio = bio;
            await _context.SaveChangesAsync();
            return ServiceResult<UserProfileDto>.Ok(MapToDto(user));
        }

        public async Task<ServiceResult<string>> UploadProfilePictureAsync(int userId, IFormFile file)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<string>.Error("User not found", 404);
            if (file == null || file.Length == 0)
                return ServiceResult<string>.Error("No file uploaded", 400);
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
                return ServiceResult<string>.Error("Invalid file type", 400);
            var uploadsDir = Path.Combine(_env.WebRootPath, "profile_pics");
            if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
            var fileName = $"user_{userId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
            user.ProfilePicture = $"/profile_pics/{fileName}";
            await _context.SaveChangesAsync();
            return ServiceResult<string>.Ok(user.ProfilePicture);
        }

        public async Task<ServiceResult<object>> DeleteProfilePictureAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return ServiceResult<object>.Error("User not found", 404);
            if (string.IsNullOrEmpty(user.ProfilePicture))
                return ServiceResult<object>.Error("No profile picture to delete", 400);
            var filePath = Path.Combine(_env.WebRootPath, user.ProfilePicture.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (File.Exists(filePath))
                File.Delete(filePath);
            user.ProfilePicture = null;
            await _context.SaveChangesAsync();
            return ServiceResult<object>.Ok(new { message = "Profile picture deleted successfully" });
        }

        private UserProfileDto MapToDto(User user) => new UserProfileDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            PhoneNumber = user.PhoneNumber,
            ProfilePicture = user.ProfilePicture,
            Bio = user.Bio,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
} 