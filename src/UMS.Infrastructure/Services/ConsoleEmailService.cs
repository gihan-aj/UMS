using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Services;

namespace UMS.Infrastructure.Services
{
    public class ConsoleEmailService : IEmailService
    {
        private readonly ILogger<ConsoleEmailService> _logger;

        public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null)
        {
            _logger.LogInformation("----- SIMULATING EMAIL SEND -----");
            _logger.LogInformation("To: {ToEmail}", toEmail);
            _logger.LogInformation("Subject: {Subject}", subject);
            _logger.LogInformation("--- HTML Body ---");
            _logger.LogInformation("{HtmlBody}", htmlBody);
            if (!string.IsNullOrWhiteSpace(plainTextBody))
            {
                _logger.LogInformation("--- Plain Text Body ---");
                _logger.LogInformation("{PlainTextBody}", plainTextBody);
            }
            _logger.LogInformation("----- END OF SIMULATED EMAIL -----");

            // Simulate successful sending
            return Task.FromResult(true);
        }
    }
}
