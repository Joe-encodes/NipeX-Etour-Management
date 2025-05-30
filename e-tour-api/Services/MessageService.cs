using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using e_tour_api.Data;
using e_tour_api.Models;
using e_tour_api.Models.DTOs;
using e_tour_api.Configuration;

namespace e_tour_api.Services
{
    public class MessageService : IMessageService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MessageService> _logger;
        private readonly byte[] _encryptionKey;

        public MessageService(AppDbContext context, ILogger<MessageService> logger, AppSettings settings)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (settings?.Encryption?.Key == null)
            {
                throw new InvalidOperationException("Encryption key is not configured");
            }
            
            try
            {
                _encryptionKey = Convert.FromBase64String(settings.Encryption.Key);
                if (_encryptionKey.Length < 32)
                {
                    throw new InvalidOperationException("Encryption key must be at least 32 bytes long");
                }
            }
            catch (FormatException)
            {
                throw new InvalidOperationException("Encryption key is not a valid base64 string");
            }
        }

        public async Task<ServiceResult<MessageDto>> SendMessageAsync(int senderId, int receiverId, string content, string? subject)
        {
            try
            {
                var sender = await _context.Users.FindAsync(senderId);
                var receiver = await _context.Users.FindAsync(receiverId);

                if (sender == null || receiver == null)
                {
                    return ServiceResult<MessageDto>.Error("Sender or receiver not found", 404);
                }

                var encryptedContent = EncryptMessage(content);
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    EncryptedContent = encryptedContent,
                    Subject = subject ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Message sent from {SenderId} to {ReceiverId}", senderId, receiverId);

                return ServiceResult<MessageDto>.Ok(MapToDto(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {SenderId} to {ReceiverId}", senderId, receiverId);
                return ServiceResult<MessageDto>.Error("Failed to send message", 500);
            }
        }

        public async Task<ServiceResult<List<MessageDto>>> GetUserMessagesAsync(int userId, bool includeRead = true)
        {
            try
            {
                var query = _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .Where(m => m.ReceiverId == userId);

                if (!includeRead)
                {
                    query = query.Where(m => !m.IsRead);
                }

                var messages = await query
                    .OrderByDescending(m => m.CreatedAt)
                    .ToListAsync();

                var decryptedMessages = messages.Select(m => MapToDto(m)).ToList();
                return ServiceResult<List<MessageDto>>.Ok(decryptedMessages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving messages for user {UserId}", userId);
                return ServiceResult<List<MessageDto>>.Error("Failed to retrieve messages", 500);
            }
        }

        public async Task<ServiceResult<MessageDto>> GetMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.Messages
                    .Include(m => m.Sender)
                    .Include(m => m.Receiver)
                    .FirstOrDefaultAsync(m => m.Id == messageId);

                if (message == null)
                {
                    return ServiceResult<MessageDto>.Error("Message not found", 404);
                }

                if (message.ReceiverId != userId && message.SenderId != userId)
                {
                    return ServiceResult<MessageDto>.Error("Unauthorized access to message", 403);
                }

                return ServiceResult<MessageDto>.Ok(MapToDto(message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving message {MessageId} for user {UserId}", messageId, userId);
                return ServiceResult<MessageDto>.Error("Failed to retrieve message", 500);
            }
        }

        public async Task<ServiceResult<object>> MarkMessageAsReadAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<object>.Error("Message not found", 404);
                }

                if (message.ReceiverId != userId)
                {
                    return ServiceResult<object>.Error("Unauthorized access to message", 403);
                }

                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return ServiceResult<object>.Ok(new object());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking message {MessageId} as read for user {UserId}", messageId, userId);
                return ServiceResult<object>.Error("Failed to mark message as read", 500);
            }
        }

        public async Task<ServiceResult<object>> DeleteMessageAsync(int messageId, int userId)
        {
            try
            {
                var message = await _context.Messages.FindAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<object>.Error("Message not found", 404);
                }

                if (message.ReceiverId != userId && message.SenderId != userId)
                {
                    return ServiceResult<object>.Error("Unauthorized access to message", 403);
                }

                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();

                return ServiceResult<object>.Ok(new object());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId} for user {UserId}", messageId, userId);
                return ServiceResult<object>.Error("Failed to delete message", 500);
            }
        }

        private string EncryptMessage(string content)
        {
            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(content);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        private string DecryptMessage(string encryptedContent)
        {
            var fullCipher = Convert.FromBase64String(encryptedContent);
            var iv = new byte[16];
            var cipher = new byte[fullCipher.Length - 16];

            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = _encryptionKey;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private MessageDto MapToDto(Message message)
        {
            if (message.Sender == null || message.Receiver == null)
            {
                throw new InvalidOperationException("Message must have both sender and receiver loaded");
            }

            return new MessageDto
            {
                Id = message.Id,
                SenderId = message.SenderId,
                SenderName = message.Sender?.Username ?? "Unknown",
                ReceiverId = message.ReceiverId,
                ReceiverName = message.Receiver?.Username ?? "Unknown",
                Content = DecryptMessage(message.EncryptedContent),
                Subject = message.Subject ?? string.Empty,
                IsRead = message.IsRead,
                CreatedAt = message.CreatedAt,
                ReadAt = message.ReadAt
            };
        }
    }
} 