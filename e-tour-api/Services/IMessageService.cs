using System.Collections.Generic;
using System.Threading.Tasks;
using e_tour_api.Models;
using e_tour_api.Models.DTOs;

namespace e_tour_api.Services
{
    public interface IMessageService
    {
        Task<ServiceResult<MessageDto>> SendMessageAsync(int senderId, int receiverId, string content, string? subject);
        Task<ServiceResult<List<MessageDto>>> GetUserMessagesAsync(int userId, bool includeRead = true);
        Task<ServiceResult<MessageDto>> GetMessageAsync(int messageId, int userId);
        Task<ServiceResult<object>> MarkMessageAsReadAsync(int messageId, int userId);
        Task<ServiceResult<object>> DeleteMessageAsync(int messageId, int userId);
    }
} 