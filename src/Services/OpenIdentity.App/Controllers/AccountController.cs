using Mango.Core.Auth;

namespace OpenIdentity.App.Controllers;

[AllowAnonymous]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOpenIddictApplicationManager applicationManager,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberLogin, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                if (!string.IsNullOrEmpty(model.ReturnUrl) && model.ReturnUrl.StartsWith("/connect/authorize"))
                {
                    return Redirect(model.ReturnUrl);
                }

                return Redirect("~/");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        ViewData["ReturnUrl"] = model.ReturnUrl;

        if (ModelState.IsValid)
        {
            var displayName = string.Join(' ', new[] { model.FirstName, model.LastName }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                EmailConfirmed = true,
                Name = string.IsNullOrWhiteSpace(displayName) ? model.Username : displayName
            };

            var result = await userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Self-registration only ever grants Customer. The role used to come from the form,
                // and Admin always exists, so anyone could sign up as Admin and pass every admin
                // check downstream. Admin accounts come from the SeedUsers configuration.
                if (!await roleManager.RoleExistsAsync(AuthRoles.Customer))
                {
                    await roleManager.CreateAsync(new IdentityRole(AuthRoles.Customer));
                }

                await userManager.AddToRoleAsync(user, AuthRoles.Customer);

                await userManager.AddClaimsAsync(user,
                [
                    new Claim(OpenIddictConstants.Claims.Name, user.Name!),
                    new Claim(OpenIddictConstants.Claims.Email, user.Email!),
                    new Claim(OpenIddictConstants.Claims.Role, AuthRoles.Customer)
                ]);

                await signInManager.SignInAsync(user, isPersistent: false);

                if (!string.IsNullOrEmpty(model.ReturnUrl) && (Url.IsLocalUrl(model.ReturnUrl) || model.ReturnUrl.StartsWith("/connect/authorize")))
                {
                    return Redirect(model.ReturnUrl);
                }

                return Redirect("~/");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult Logout(string logoutId)
    {
        return View(new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(LogoutViewModel model)
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        var postLogoutRedirectUri = request?.PostLogoutRedirectUri ?? "~/";

        string clientName = string.Empty;
        var audienceClaim = User.FindFirst("aud")?.Value;
        if (!string.IsNullOrEmpty(audienceClaim))
        {
            var application = await applicationManager.FindByClientIdAsync(audienceClaim);
            if (application != null)
            {
                clientName = await applicationManager.GetDisplayNameAsync(application) ?? string.Empty;
            }
            else
            {
                logger.LogWarning("Could not resolve client display name for audience '{Audience}'.", audienceClaim);
            }
        }

        await signInManager.SignOutAsync();

        return View("LoggedOut", new LoggedOutViewModel
        {
            PostLogoutRedirectUri = postLogoutRedirectUri,
            ClientName = clientName,
            SignOutIframeUrl = ""
        });
    }

    [HttpGet("~/connect/endsession")]
    public IActionResult EndSession()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        var logoutId = request?.GetParameter("logout_id")?.ToString() ?? string.Empty;

        return View("Logout", new LogoutViewModel
        {
            LogoutId = logoutId,
            ShowLogoutPrompt = true
        });
    }

    [HttpPost("~/connect/endsession")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndSessionPost()
    {
        var request = HttpContext.GetOpenIddictServerRequest();
        var postLogoutRedirectUri = request?.PostLogoutRedirectUri ?? "~/";

        await signInManager.SignOutAsync();

        return SignOut(
            new AuthenticationProperties { RedirectUri = postLogoutRedirectUri },
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
