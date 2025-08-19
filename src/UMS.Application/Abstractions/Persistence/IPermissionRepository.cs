using System;
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

        Task<List<Permission>> GetPermissionsByClientIdAsync(
            Guid ClientId, 
            CancellationToken cancellationToken = default);

        //Task<short> GetTheLastId(CancellationToken cancellationToken = default);

        Task AddRangeAsync(
            List<Permission> permissions, 
            CancellationToken cancellationToken = default);

        Task<List<Permission>> GetPermissionsByIdsAsync(
            List<short> permissionIds, 
            CancellationToken cancellationToken);

        void RemoveRange(List<Permission> permissions);

        Task<bool> IsAnyPermissionsInUse(
            List<short> permissionIds, 
            CancellationToken cancellationToken = default);

        Task<List<short>> GetInUsePermissionIds(
            List<short> permissionIds,
            CancellationToken cancellationToken = default);
    }
}
