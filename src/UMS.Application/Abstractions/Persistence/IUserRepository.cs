using UMS.Domain.Users;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);

        Task AddAsync(User user);

        Task<bool> ExistsByEmailAsync(string email);
    }
}
