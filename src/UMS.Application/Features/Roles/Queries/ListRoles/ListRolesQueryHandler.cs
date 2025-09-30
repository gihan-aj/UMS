using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.SharedKernel;

namespace UMS.Application.Features.Roles.Queries.ListRoles
{
    public class ListRolesQueryHandler : IQueryHandler<ListRolesQuery, PagedList<RoleResponse>>
    {
        private readonly IRoleRepository _roleRepository;

        public ListRolesQueryHandler(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<Result<PagedList<RoleResponse>>> Handle(ListRolesQuery request, CancellationToken cancellationToken)
        {
            var pagedRolesList = await _roleRepository.ListAsync(
                request.Query,
                cancellationToken);

            var roleResponse = pagedRolesList.Items
                .Select(r => new RoleResponse(r.Id, r.Name, r.Description))
                .ToList();

            var pagedResponse = new PagedList<RoleResponse>(
                roleResponse, 
                pagedRolesList.Page, 
                pagedRolesList.PageSize, 
                pagedRolesList.TotalCount);

            return pagedResponse;
        }
    }
}
