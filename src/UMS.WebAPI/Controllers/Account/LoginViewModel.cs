using System.ComponentModel.DataAnnotations;

namespace UMS.WebAPI.Controllers.Account
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public bool RememberLogin { get; set; }

        public string ReturnUrl { get; set; } = string.Empty;
    }
}
