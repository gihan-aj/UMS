using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Roles.Queries.ListQueries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.GetAllRoles
{
    public class GetAllRolesQueryHandler : IQueryHandler<GetAllRolesQuery, List<RoleResponse>>
    {
        private readonly IRoleRepository _roleRepository;

        public GetAllRolesQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Result<List<RoleResponse>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            var roles = await _roleRepository.GetAllAsync(cancellationToken);
            if(roles is null)
            {
                return new List<RoleResponse>();
            }
            else
            {
                return roles.Select(r => new RoleResponse(r.Id, r.Name)).ToList();
            }
        }
    }
}
