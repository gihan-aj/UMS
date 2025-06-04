using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UMS.Application.Abstractions.Services;
using UMS.Domain.Users;
using UMS.Infrastructure.Authentication.Settings;

namespace UMS.Infrastructure.Services
{
    public class JwtTokenGeneratorService : IJwtTokenGeneratorService
    {
        private readonly JwtSettings _jwtSettings;

        public JwtTokenGeneratorService(IOptions<JwtSettings> jwtOptions)
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
        }

        public (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

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
    }
}
