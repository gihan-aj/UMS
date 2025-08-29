using Duende.IdentityServer;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using UMS.Application.Abstractions.Persistence;
using UMS.Application.Abstractions.Services;

namespace UMS.WebAPI.Controllers.Account
{
    [AllowAnonymous]
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
        public IActionResult Login(string returnUrl)
        {
            var vm = new LoginViewModel { ReturnUrl = returnUrl };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetByEmailAsync(model.Username);
                if( user != null && user.IsActive && !user.IsDeleted && _passwordHasher.VerifyPassword
                    (model.Password, user.PasswordHash!))
                {
                    // Create the authentication cookie
                    await HttpContext.SignInAsync(
                        new IdentityServerUser(user.Id.ToString())
                        {
                            DisplayName = user.Email
                        },
                        new AuthenticationProperties
                        {
                            IsPersistent = model.RememberLogin
                        });

                    // Redirect back to the client application
                    return Redirect(model.ReturnUrl);
                }

                ModelState.AddModelError(string.Empty, "Invalid username or password");
            }

            return View(model);
        }
    }
}
