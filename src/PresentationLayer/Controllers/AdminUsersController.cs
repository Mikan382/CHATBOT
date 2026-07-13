using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : BaseController
{
    private static readonly string[] Roles = ["Student", "Teacher", "Admin"];
    private readonly IUserAdminService _userAdminService;

    public AdminUsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? searchTerm, string? role, CancellationToken cancellationToken)
    {
        return View(await BuildIndexModelAsync(searchTerm, role, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind(Prefix = "CreateUser")] CreateUserInput input,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildIndexModelAsync(null, null, cancellationToken);
            model.CreateUser = input;
            return View("Index", model);
        }

        try
        {
            await _userAdminService.CreateAsync(input.Email, input.FullName, input.Role, input.Password, cancellationToken);
            SetFlashSuccess("User was created.");
        }
        catch (InvalidOperationException ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateUserInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            SetFlashError("Please check the user fields.");
            return RedirectToAction("Index");
        }

        try
        {
            var requiresRelogin = await _userAdminService.UpdateAsync(
                CurrentUserId(),
                input.Id,
                input.Email,
                input.FullName,
                input.Role,
                cancellationToken);
            SetFlashSuccess("User was updated.");
            if (requiresRelogin)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }
        }
        catch (InvalidOperationException ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(Guid userId, CancellationToken cancellationToken)
    {
        await ChangeLockoutAsync(userId, true, cancellationToken);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid userId, CancellationToken cancellationToken)
    {
        await ChangeLockoutAsync(userId, false, cancellationToken);
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _userAdminService.DeleteAsync(CurrentUserId(), userId, cancellationToken);
            SetFlashSuccess("User was deleted.");
        }
        catch (InvalidOperationException ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid userId, string newPassword, CancellationToken cancellationToken)
    {
        try
        {
            await _userAdminService.ResetPasswordAsync(CurrentUserId(), userId, newPassword, cancellationToken);
            SetFlashSuccess("Password was reset.");
        }
        catch (InvalidOperationException ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    private async Task ChangeLockoutAsync(Guid userId, bool locked, CancellationToken cancellationToken)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(CurrentUserId(), userId, locked, cancellationToken);
            SetFlashSuccess(locked ? "User was locked." : "User was unlocked.");
        }
        catch (InvalidOperationException ex)
        {
            SetFlashError(ex.Message);
        }
    }

    private async Task<UserAdminIndexViewModel> BuildIndexModelAsync(
        string? searchTerm,
        string? role,
        CancellationToken cancellationToken)
    {
        return new UserAdminIndexViewModel
        {
            Users = await _userAdminService.ListAsync(searchTerm, role, cancellationToken),
            Roles = Roles,
            SearchTerm = searchTerm,
            SelectedRole = role,
            CurrentUserId = CurrentUserId()
        };
    }
}
