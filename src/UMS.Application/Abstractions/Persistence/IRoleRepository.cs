using System.Threading;
using System.Threading.Tasks;
using UMS.Domain.Authorization;
using UMS.SharedKernel;

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
        Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        Task<PagedList<Role>> GetPagedListAsync(
            int page,
            int pageSize,
            string? searchTerm,
            CancellationToken cancellationToken = default);

        Task AddAsync(Role role, CancellationToken cancellationToken = default);

        void Update(Role role);

        Task<byte> GetNextIdAsync();

        Task<Role?> GetByIdWithPermissionsAsync(byte id, CancellationToken cancellationToken = default);

        Task<Role?> GetByIdAsync(byte id);
    }
}
