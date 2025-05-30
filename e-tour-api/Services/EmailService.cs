using System;
using System.Threading.Tasks;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using e_tour_api.Configuration;

namespace e_tour_api.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string email, string token);
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUsername;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _baseUrl;

        public EmailService(
            AppSettings settings,
            ILogger<EmailService> logger)
        {
            _logger = logger;

            _smtpServer = settings.Email.SmtpServer ?? throw new ArgumentNullException("SMTP Server configuration is missing");
            _smtpPort = settings.Email.SmtpPort;
            _smtpUsername = settings.Email.SmtpUsername ?? throw new ArgumentNullException("SMTP Username configuration is missing");
            _smtpPassword = settings.Email.SmtpPassword ?? throw new ArgumentNullException("SMTP Password configuration is missing");
            _fromEmail = settings.Email.FromEmail ?? throw new ArgumentNullException("From Email configuration is missing");
            _baseUrl = settings.Email.BaseUrl;
        }

        public async Task SendVerificationEmailAsync(string email, string token)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(_smtpUsername, _smtpPassword)
                };

                var verificationLink = $"{_baseUrl}/verify-email?token={token}";
                var message = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = "Verify Your Email Address",
                    Body = $@"
                        <h2>Welcome to NipeX E-Tour Management!</h2>
                        <p>Please click the link below to verify your email address:</p>
                        <p><a href='{verificationLink}'>Verify Email Address</a></p>
                        <p>If you did not create an account, please ignore this email.</p>
                        <p>This link will expire in 24 hours.</p>",
                    IsBodyHtml = true
                };
                message.To.Add(email);

                await client.SendMailAsync(message);
                _logger.LogInformation("Verification email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", email);
                throw new EmailServiceException("Failed to send verification email", ex);
            }
        }
    }

    public class EmailServiceException : Exception
    {
        public EmailServiceException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
} 