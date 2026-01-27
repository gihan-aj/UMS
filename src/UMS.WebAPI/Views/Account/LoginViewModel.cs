namespace UMS.WebAPI.Views.Account
{
    // Represents the data needed to display the login form.
    public class LoginViewModel : LoginInputModel
    {
        public LoginViewModel() { }
        public LoginViewModel(LoginInputModel other)
        {
            Email = other.Email;
            Password = other.Password;
            ReturnUrl = other.ReturnUrl;
        }
    }

    // Represents the data submitted from the login form.
    public class LoginInputModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ReturnUrl { get; set; } = string.Empty;
    }
}
