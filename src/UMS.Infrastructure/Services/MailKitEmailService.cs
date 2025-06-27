using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;
using UMS.Infrastructure.Settings;

namespace UMS.Infrastructure.Services
{
    public class MailKitEmailService : IEmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<MailKitEmailService> _logger;

        public MailKitEmailService(IOptions<SmtpSettings> smtpSettings, ILogger<MailKitEmailService> logger)
        {
            _smtpSettings = smtpSettings.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(
            string toEmail, 
            string subject, 
            string htmlBody, 
            string? plainTextBody = null)
        {
            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromAddress));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = htmlBody,
                    TextBody = plainTextBody?? "" // Fallback to empty string if null
                };
                email.Body = bodyBuilder.ToMessageBody();

                using var smtpClient = new SmtpClient();

                // Use SecureSocketOptions.StartTls for ports like 587.
                // Use SecureSocketOptions.SslOnConnect for ports like 465.
                var secureSocketOptions = _smtpSettings.UseSsl
                    ? SecureSocketOptions.StartTls 
                    : SecureSocketOptions.SslOnConnect;

                _logger.LogInformation("Connecting to SMTP server {Host} on port {Port}...", _smtpSettings.Host, _smtpSettings.Port);
                await smtpClient.ConnectAsync(
                    _smtpSettings.Host,
                    _smtpSettings.Port,
                    secureSocketOptions);

                _logger.LogInformation("Authenticating with SMTP server...");
                await smtpClient.AuthenticateAsync(
                    _smtpSettings.Username,
                    _smtpSettings.Password);

                _logger.LogInformation("Sending email to {ToEmail} with subject '{Subject}'...", toEmail, subject);
                await smtpClient.SendAsync(email);

                _logger.LogInformation("Email sent successfully.");
                await smtpClient.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'.", toEmail, subject);
                return false;
            }
        }
    }
}
