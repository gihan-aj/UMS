using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UMS.Application.Abstractions.Services
{
    /// <summary>
    /// Defines the contract for a service that sends emails.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email asynchronously.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="subject">The subject of the email.</param>
        /// <param name="htmlBody">The HTML content of the email body.</param>
        /// <param name="plainTextBody">Optional: The plain text content of the email body for clients that don't support HTML.</param>
        /// <returns>A task representing the asynchronous operation. The task result indicates whether the email was sent successfully (or queued successfully by the underlying service).</returns>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody, string? plainTextBody = null);
    }
}
