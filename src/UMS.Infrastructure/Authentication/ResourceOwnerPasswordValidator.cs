using System.Threading.Tasks;
using Duende.IdentityModel;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;

namespace UMS.Infrastructure.Authentication
{
    /// <summary>
    /// Handles the resource owner password grant type.
    /// This is where we validate the user's username and password using our custom services.
    /// </summary>
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasherService;

        public ResourceOwnerPasswordValidator(IUserRepository userRepository, IPasswordHasherService passwordHasherService)
        {
            _userRepository = userRepository;
            _passwordHasherService = passwordHasherService;
        }

        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var user = await _userRepository.GetByEmailAsync(context.UserName);
            if( user != null && user.IsActive && !user.IsDeleted)
            {
                if(_passwordHasherService.VerifyPassword(context.Password, user.PasswordHash!))
                {
                    // Validation successful
                    context.Result = new GrantValidationResult(
                        subject: user.Id.ToString(),
                        authenticationMethod: OidcConstants.AuthenticationMethods.Password);

                    // Optionally, record the login
                    user.RecordLogin();
                    // Note: We would need to save this change. This highlights a limitation of this flow
                    // if you want to track logins during a token request. A full interactive flow is better.
                    return;
                }
            }

            // Validation failed
            context.Result = new GrantValidationResult(
                TokenRequestErrors.InvalidGrant,
                "Invalid username or password");
        }
    }
}
