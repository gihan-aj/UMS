using System.Threading.Tasks;
using UMS.Domain.Authorization;

namespace UMS.Application.Abstractions.Persistence
{
    /// <summary>
    /// Interface for repository operations related to the Role entity.
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// Retrieves a role by its name.
        /// </summary>
        /// <param name="name">The name of the role to search for.</param>
        /// <returns>The Role if found; otherwise, null.</returns>
        Task<Role?> GetByNameAsync(string name);
    }
}
