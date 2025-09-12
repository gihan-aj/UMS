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
    public class ListUsersQueryHandler : IQueryHandler<ListUsersQuery, PagedList<UserProfileResponse>>
    {
        private readonly IUserRepository _userRepository;

        public ListUsersQueryHandler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<PagedList<UserProfileResponse>>> Handle(ListUsersQuery request, CancellationToken cancellationToken)
        {
            //var pagedUserList = await _userRepository.GetPagedListAsync(
            //    request.Page, 
            //    request.PageSize, 
            //    request.SerchTerm, 
            //    cancellationToken);
            
            var pagedUserList = await _userRepository.ListAsync(
                request.Query, 
                cancellationToken);

            // Mapping from the paginated domain entity (User) to the paginated DTO (UserProfileResponse)
            var userProfileResponses = pagedUserList.Items
                .Select(u => new UserProfileResponse(
                    u.Id,
                    u.UserCode,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.IsActive,
                    u.CreatedAtUtc,
                    u.LastLoginAtUtc
                )).ToList();

            // Create a new PagedList of the response type
            var pagedResponse = new PagedList<UserProfileResponse>(
                userProfileResponses,
                pagedUserList.Page,
                pagedUserList.PageSize,
                pagedUserList.TotalCount
            );

            return pagedResponse;
        }
    }
}
