using Microsoft.AspNetCore.Http;
using e_tour_api.Models.DTOs;
using e_tour_api.Models;

namespace e_tour_api.Services
{
    public interface IUserService
    {
        Task<ServiceResult<UserProfileDto>> GetUserProfileAsync(int userId);
        Task<ServiceResult<UserProfileDto>> UpdateUserProfileAsync(int userId, string? phoneNumber, string? bio);
        Task<ServiceResult<string>> UploadProfilePictureAsync(int userId, IFormFile file);
        Task<ServiceResult<object>> DeleteProfilePictureAsync(int userId);
    }
} 