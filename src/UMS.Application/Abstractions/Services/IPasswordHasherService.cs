namespace UMS.Application.Abstractions.Services
{
    public interface IPasswordHasherService
    {
        string HashPassword(string password);

        bool VerifyPassword(string password, string hashedPassword);
    }
}
