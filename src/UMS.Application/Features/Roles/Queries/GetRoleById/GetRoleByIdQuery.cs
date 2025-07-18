﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.GetRoleById
{
    /// <summary>
    /// Query to get a single role by its ID, including its assigned permissions.
    /// </summary>
    public sealed record GetRoleByIdQuery(byte RoleId) : IQuery<RoleWithPermissionsResponse>;

    public class GetRoleByIdQueryHandler : IQueryHandler<GetRoleByIdQuery, RoleWithPermissionsResponse>
    {
        private readonly IRoleRepository _roleRepository;

        public GetRoleByIdQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Result<RoleWithPermissionsResponse>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdWithPermissionsAsync(request.RoleId, cancellationToken);
            if(role is null)
            {
                return Result.Failure<RoleWithPermissionsResponse>(new Error(
                    "Role.NotFound",
                    $"Role id with {request.RoleId} was not found",
                    ErrorType.NotFound));
            }

            // Map the domain entities to the response DTOs
            var permissionResponse = role.Permissions
                .Select(rp => new PermissionResponse(rp.Permission.Id, rp.Permission.Name))
                .ToList();

            var response = new RoleWithPermissionsResponse(
                role.Id,
                role.Name,
                permissionResponse);

            return response;
        }
    }
}
