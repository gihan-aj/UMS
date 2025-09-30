using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Duende.IdentityModel;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.EntityFrameworkCore;
using UMS.Application.Abstractions.Persistence;
using UMS.Infrastructure.Persistence;

namespace UMS.Infrastructure.Authentication
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _dbContext;

        public ProfileService(IUserRepository userRepository, ApplicationDbContext dbContext)
        {
            _userRepository = userRepository;
            _dbContext = dbContext;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var userId = context.Subject.GetSubjectId();
            if(!Guid.TryParse(userId, out var userGuid))
            {
                return;
            }

            var user = await _userRepository.GetByIdAsync(userGuid, default);
            if (user == null)
            {
                return;
            }

            var claims = new List<Claim>
            {
                new Claim(JwtClaimTypes.Subject, user.Id.ToString()),
                new Claim(JwtClaimTypes.Email, user.Email),
                new Claim("userCode", user.UserCode ?? string.Empty),
                // Add other standard claims like name, etc.
            };

            var userRoles = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .Select(ur => ur.Role.Name)
                .ToListAsync();

             var userPermissions = await _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Include(ur => ur.Role)
                .ThenInclude(ur => ur.RolePermissions)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();

            foreach (var roleName in userRoles)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, roleName));
            }
            foreach (var permission in userPermissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            context.IssuedClaims = claims;
        }

        public async Task IsActiveAsync(IsActiveContext context)
        {
            var userId = context.Subject.GetSubjectId();
            if (!Guid.TryParse(userId, out var userGuid))
            {
                context.IsActive = false;
                return;
            }

            var user = await _userRepository.GetByIdAsync(userGuid, default);
            context.IsActive = user != null && user.IsActive && !user.IsDeleted;
        }
    }
}
