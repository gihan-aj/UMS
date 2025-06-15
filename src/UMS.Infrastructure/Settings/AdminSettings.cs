namespace UMS.Infrastructure.Settings
{
    public class AdminSettings
    {
        public const string SectionName = "AdminSettings";
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = "Super";
        public string LastName { get; set; } = "Admin";
    }
}
