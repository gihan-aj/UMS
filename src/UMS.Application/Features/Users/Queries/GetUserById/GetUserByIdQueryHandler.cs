using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Permissions.Queries.ListPermissions;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Queries.GetUserById
{
    public class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, UserDetailResponse>
    {
        private readonly IUserRepository _userRepository;

        public GetUserByIdQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserDetailResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdWithRolesAndPermissionsAsync(request.Id, cancellationToken);

            if (user is null)
            {
                return Result.Failure<UserDetailResponse>(new Error(
                    "User.NotFound",
                    $"The user with ID {request.Id} was not found.",
                    ErrorType.NotFound));
            }

            // Map the roles to the response DTO
            var assignedRoles = user.UserRoles
                .Select(ur => new AssignedRoleResponse(ur.Role.Id, ur.Role.Name))
                .ToList();

            // Flatten all permissions from all roles, get distinct ones, and map them
            var distinctPermissions = user.UserRoles
                .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission))
                .Distinct()
                .Select(p => new PermissionDetailResponse(p.Name, GenerateDescription(p.Name)))
                .OrderBy(p => p.Name)
                .ToList();

            var response = new UserDetailResponse(
            user.Id,
            user.UserCode,
                user.Email,
                user.FirstName ?? "",
                user.LastName ?? "",
                user.IsActive,
                user.CreatedAtUtc,
                assignedRoles,
                distinctPermissions);

            return response;
        }

        private static string GenerateDescription(string permissionName)
        {
            var parts = permissionName.Split(':');
            if (parts.Length != 2) return permissionName;
            var action = parts[1].Replace("_", " ");
            var resource = parts[0];
            return $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(action)} {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(resource)}";
        }
    }
}
