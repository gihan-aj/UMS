namespace UMS.Application.Settings
{
    public class ClientAppSettings
    {
        public const string SectionName = "ClientAppSettings";

        public string ActivationLinkBaseUrl { get; set; } = string.Empty;

        public string PasswordResetLinkBaseUrl {  get; set; } = string.Empty;
    }
}
