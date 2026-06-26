using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = UserRoleNames.Admin)]
public class AdminUsersController : Controller
{
    private readonly UserAdminService _userAdminService;

    public AdminUsersController(UserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet("/admin/users")]
    public async Task<IActionResult> Index()
    {
        return View(new UserAdminIndexViewModel
        {
            Users = await _userAdminService.ListAsync()
        });
    }

    [HttpGet("/admin/users/create")]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/users/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserAdminIndexViewModel input)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", new UserAdminIndexViewModel
            {
                Users = await _userAdminService.ListAsync(),
                CreateUser = input.CreateUser
            });
        }

        try
        {
            await _userAdminService.CreateAsync(
                input.CreateUser.Email,
                input.CreateUser.FullName,
                input.CreateUser.Role,
                input.CreateUser.Password);
            TempData["Success"] = "User was created.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/admin/users/{id:guid}/role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid id, string role)
    {
        try
        {
            await _userAdminService.ChangeRoleAsync(id, role);
            TempData["Success"] = "User role was updated.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/admin/users/{id:guid}/lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLockout(Guid id, bool locked)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(id, locked);
            TempData["Success"] = locked ? "User was locked." : "User was unlocked.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost("/admin/users/{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _userAdminService.DeleteAsync(id);
            TempData["Success"] = "User was deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("/admin/users/{id:guid}/reset-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, string newPassword)
    {
        try
        {
            await _userAdminService.ResetPasswordAsync(id, newPassword);
            TempData["Success"] = "Password was reset.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
