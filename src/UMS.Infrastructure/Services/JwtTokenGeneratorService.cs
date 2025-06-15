using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using UMS.Application.Abstractions.Services;
using UMS.Domain.Users;
using UMS.Infrastructure.Authentication.Settings;
using UMS.Infrastructure.Persistence;

namespace UMS.Infrastructure.Services
{
    public class JwtTokenGeneratorService : IJwtTokenGeneratorService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ApplicationDbContext _dbContext;

        public JwtTokenGeneratorService(IOptions<JwtSettings> jwtOptions, ApplicationDbContext dbContext)
        {
            _jwtSettings = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions), "JWT settings cannot be null.");

            if (string.IsNullOrEmpty(_jwtSettings.Secret))
                throw new ArgumentNullException(nameof(_jwtSettings.Secret), "JWT Secret cannot be null or empty.");
            if (string.IsNullOrEmpty(_jwtSettings.Issuer))
                throw new ArgumentNullException(nameof(_jwtSettings.Issuer), "JWT Issuer cannot be null or empty.");
            if (string.IsNullOrEmpty(_jwtSettings.Audience))
                throw new ArgumentNullException(nameof(_jwtSettings.Audience), "JWT Audience cannot be null or empty.");
            if (_jwtSettings.ExpiryMinutes <= 0)
                throw new ArgumentOutOfRangeException(nameof(_jwtSettings.ExpiryMinutes), "JWT ExpiryMinutes must be greater than zero.");
            _dbContext = dbContext;
        }

        public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

            // --- Fetch Roles and Permissions
            // This is a simplified query. For performance, this could be optimized
            // or cached. It runs inside the login transaction.
            var userRoles = _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                .ToList();

            var userPermissions = _dbContext.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(_dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
                .Join(_dbContext.Permissions, pid => pid, p => p.Id, (pid, p) => p.Name)
                .Distinct()
                .ToList();

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token id
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject (User id)
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("uid", user.Id.ToString()), // Custom claim for user ID (often 'nameidentifier' or 'sub' is used.)
                new Claim("userCode", user.UserCode ?? string.Empty)
                // Add other claims as needed, e.g., roles
                // new Claim(ClaimTypes.Role, "Admin"),
                // new Claim(ClaimTypes.Role, "User"),
            };

            // Add role claims
            foreach (var roleName in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, roleName));
            }

            // Add permission claims (custom claim type)
            foreach(var permission in userPermissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            if (!string.IsNullOrWhiteSpace(user.FirstName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName));
            }

            if (!string.IsNullOrWhiteSpace(user.LastName))
            {
                claims.Add(new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            };

            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            string token = tokenHandler.WriteToken(securityToken);

            return (token, tokenDescriptor.Expires.Value);
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ValidateLifetime = false, // IMPORTANT: We don't validate the token's lifetime here
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if(jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Token is not valid or uses different algorithm
                    return null;
                }

                return principal;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
