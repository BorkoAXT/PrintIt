// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using PrintIt.Models;

namespace PrintIt.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly IUserStore<User> _userStore;
        private readonly IUserEmailStore<User> _emailStore;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<ExternalLoginModel> _logger;

        public ExternalLoginModel(
            SignInManager<User> signInManager,
            UserManager<User> userManager,
            IUserStore<User> userStore,
            ILogger<ExternalLoginModel> logger,
            IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _logger = logger;
            _emailSender = emailSender;
        }

        // Kept for compatibility with the scaffolded UI, but we won't use the confirmation page anymore.
        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }
        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public IActionResult OnPost(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // If the user already has a login, just sign them in.
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: false,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.",
                    info.Principal.Identity?.Name, info.LoginProvider);

                return LocalRedirect(returnUrl);
            }

            if (signInResult.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }

            // ✅ NEW USER FLOW:
            // Auto-create the local user from the external provider (no "enter email" / "Register" page).
            var email =
                info.Principal.FindFirstValue(ClaimTypes.Email)
                ?? info.Principal.FindFirstValue("email");

            if (string.IsNullOrWhiteSpace(email))
            {
                ErrorMessage = "Email not received from the external provider.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // If a local user already exists with this email, you can choose to link automatically.
            // We'll try to link to the existing user to avoid duplicates.
            var existingByEmail = await _userManager.FindByEmailAsync(email);
            if (existingByEmail != null)
            {
                var linkResult = await _userManager.AddLoginAsync(existingByEmail, info);
                if (linkResult.Succeeded)
                {
                    await _signInManager.SignInAsync(existingByEmail, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }

                // If linking failed, show a useful error.
                ErrorMessage = "A local account with this email already exists, but we couldn't link the external login.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var user = CreateUser();

            // Email
            await _emailStore.SetEmailAsync(user, email, CancellationToken.None);

            // Skip confirmation for external providers (Google)
            user.EmailConfirmed = true;

            // Username = Google display name (for NEW users only)
            var displayName =
                info.Principal.FindFirstValue("name")
                ?? info.Principal.FindFirstValue(ClaimTypes.Name)
                ?? info.Principal.FindFirstValue("given_name")
                ?? email.Split('@')[0];

            static string NormalizeUserName(string s)
            {
                s = (s ?? "").Trim();
                var sb = new StringBuilder(s.Length);

                // Keep it simple: letters/digits/_/./-
                foreach (var ch in s)
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_' || ch == '.' || ch == '-')
                        sb.Append(ch);
                }

                return sb.ToString();
            }

            var baseUserName = NormalizeUserName(displayName);
            if (string.IsNullOrWhiteSpace(baseUserName))
                baseUserName = NormalizeUserName(email.Split('@')[0]);

            // Ensure unique username
            var userName = baseUserName;
            var i = 1;
            while (await _userManager.FindByNameAsync(userName) != null)
            {
                userName = $"{baseUserName}{i++}";
            }

            await _userStore.SetUserNameAsync(user, userName, CancellationToken.None);

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    _logger.LogWarning("External login user creation error: {Code} - {Description}",
                        error.Code, error.Description);
                }

                ErrorMessage = "Could not create a local user account from the external login.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var addLoginResult = await _userManager.AddLoginAsync(user, info);
            if (!addLoginResult.Succeeded)
            {
                foreach (var error in addLoginResult.Errors)
                {
                    _logger.LogWarning("External login linking error: {Code} - {Description}",
                        error.Code, error.Description);
                }

                // Clean up the user we just created to avoid orphaned accounts
                await _userManager.DeleteAsync(user);

                ErrorMessage = "Could not link the external login to the created user account.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);

            await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        // Not used anymore (we auto-create users in the callback), but left so routes don't 404 if something posts here.
        public async Task<IActionResult> OnPostConfirmationAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            return LocalRedirect(returnUrl);
        }

        private User CreateUser()
        {
            try
            {
                return Activator.CreateInstance<User>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(User)}'. " +
                    $"Ensure that '{nameof(User)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the external login page in /Areas/Identity/Pages/Account/ExternalLogin.cshtml");
            }
        }

        private IUserEmailStore<User> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<User>)_userStore;
        }
    }
}
