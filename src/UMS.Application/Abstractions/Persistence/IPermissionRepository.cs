using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UMS.Domain.Authorization;

namespace UMS.Application.Abstractions.Persistence
{
    public interface IPermissionRepository
    {
        Task<List<Permission>> GetPermissionsByNameRangeAsync(
            List<string> permissionNames, 
            CancellationToken cancellationToken = default);
    }
}
