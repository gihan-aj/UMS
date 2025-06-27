﻿namespace UMS.Infrastructure.Settings
{
    /// <summary>
    /// Configuration settings for connecting to an SMTP server.
    /// </summary>
    public class SmtpSettings
    {
        public const string SectionName = "SmtpSettings";

        public string Host {  get; set; } = string.Empty;

        public int Port { get; set; } = 587;

        public string Username {  get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FromAddress {  get; set; } = string.Empty;

        public string FromName { get; set; } = "UMS Application";

        public bool UseSsl { get; set; } = true;
    }
}
