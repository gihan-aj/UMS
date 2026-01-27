using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;
using UMS.WebAPI.Views.Account;

namespace UMS.WebAPI.Controllers.Account
{
    public class AccountController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasherService _passwordHasher;

        public AccountController(
            IIdentityServerInteractionService interaction,
            IUserRepository userRepository,
            IPasswordHasherService passwordHasher)
        {
            _interaction = interaction;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl)
        {
            var vm = new LoginViewModel { ReturnUrl = returnUrl };
            return View(vm);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // Check if the user is trying to log in to a specific client app
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
            if (context == null)
            {
                // If not part of an SSO flow, handle as a direct login if needed, or show error.
                // For now, we'll assume it's always part of an SSO flow.
                ModelState.AddModelError(string.Empty, "Invalid login request.");
                return View(new LoginViewModel { ReturnUrl = model.ReturnUrl });
            }

            if (ModelState.IsValid)
            {
                // Use our custom services to validate the user
                var user = await _userRepository.GetByEmailAsync(model.Email);
                if (user != null && user.IsActive && !user.IsDeleted && _passwordHasher.VerifyPassword(model.Password, user.PasswordHash!))
                {
                    // Fix for CS0117: Replace 'IdentityServerConstants.JwtClaimTypes.Subject' with 'ClaimTypes.NameIdentifier'
                    // Fix for CS1503: Ensure the second argument of the Claim constructor is a string, which it already is
                    // Fix for IDE0090: Simplify 'new' expressions where applicable

                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.Id.ToString()), // Use ClaimTypes.NameIdentifier for the 'sub' claim
                        new(ClaimTypes.Email, user.Email) // Use ClaimTypes.Email for the email claim
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // Sign in the user to create the authentication cookie
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    // Redirect back to IdentityServer to complete the SSO flow
                    return Redirect(model.ReturnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid username or password.");
            }

            // If we got this far, something failed, redisplay form
            return View(new LoginViewModel(model));
        }
    }
}
