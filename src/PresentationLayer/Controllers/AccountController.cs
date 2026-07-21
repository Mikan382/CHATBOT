using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.DTOs;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

public class AccountController : BaseController
{
    private readonly IAuthService _authService;

    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Chat");
        }

        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _authService.AuthenticateAsync(model.Email, model.Password, cancellationToken);
            await SignInAsync(user, model.RememberMe);
            if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Chat");
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            model.Password = "";
            ModelState.Remove(nameof(LoginViewModel.Password));
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Chat");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Chat");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var user = await _authService.RegisterAsync(model.Email, model.FullName, model.Password, cancellationToken);
            await SignInAsync(user, rememberMe: false);
            SetFlashSuccess("Registration successful! Welcome to PRN222 Course Assistant.");
            return RedirectToAction("Index", "Chat");
        }
        catch (Exception ex)
        {
            model.Error = UserFacingError(ex);
            model.Password = "";
            model.ConfirmPassword = "";
            ModelState.Remove(nameof(RegisterViewModel.Password));
            ModelState.Remove(nameof(RegisterViewModel.ConfirmPassword));
            return View(model);
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await _authService.ChangePasswordAsync(
            CurrentUserId(),
            model.CurrentPassword,
            model.NewPassword,
            cancellationToken);
        if (!success)
        {
            model.Error = error ?? "Password change failed.";
            model.CurrentPassword = "";
            model.NewPassword = "";
            model.ConfirmPassword = "";
            ModelState.Remove(nameof(ChangePasswordViewModel.CurrentPassword));
            ModelState.Remove(nameof(ChangePasswordViewModel.NewPassword));
            ModelState.Remove(nameof(ChangePasswordViewModel.ConfirmPassword));
            return View(model);
        }

        SetFlashSuccess("Password changed successfully.");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    private async Task SignInAsync(AuthenticatedUserDto user, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("user_version", user.UpdatedAtUtc.Ticks.ToString())
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(14) : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}
