using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Common.Messaging.Queries;
using UMS.Application.Features.Users.Queries.GetMyProfile;
using UMS.SharedKernel;

namespace UMS.Application.Features.Users.Queries.ListUsers
{
    public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, List<UserProfileResponse>>
    {
        private readonly IUserRepository _userRepository;

        public ListUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<List<UserProfileResponse>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetAllUsersAsync(cancellationToken);
            var response = users
                .Select(u => new UserProfileResponse(
                    u.Id,
                    u.UserCode,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.CreatedAtUtc,
                    u.LastLoginAtUtc
                ))
                .ToList();

            return response;
        }
    }
}
