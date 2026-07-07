using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Controllers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : BaseController
{
    private readonly IUserAdminService _userAdminService;

    public AdminUsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _userAdminService.ListAsync();
        var model = new UserAdminIndexViewModel
        {
            Users = users,
            Roles = ["Student", "Teacher", "Admin"]
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateUser")] CreateUserInput input)
    {
        if (!ModelState.IsValid)
        {
            var users = await _userAdminService.ListAsync();
            return View("Index", new UserAdminIndexViewModel { Users = users, Roles = ["Student", "Teacher", "Admin"], CreateUser = input });
        }

        try
        {
            await _userAdminService.CreateAsync(input.Email, input.FullName, input.Role, input.Password);
            SetFlashSuccess("User was created.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid userId, string role)
    {
        try
        {
            await _userAdminService.ChangeRoleAsync(userId, role);
            SetFlashSuccess("User role was updated.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lock(Guid userId)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(userId, true);
            SetFlashSuccess("User was locked.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unlock(Guid userId)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(userId, false);
            SetFlashSuccess("User was unlocked.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid userId)
    {
        try
        {
            await _userAdminService.DeleteAsync(userId);
            SetFlashSuccess("User was deleted.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid userId, string newPassword)
    {
        try
        {
            await _userAdminService.ResetPasswordAsync(userId, newPassword);
            SetFlashSuccess("Password was reset.");
        }
        catch (Exception ex)
        {
            SetFlashError(ex.Message);
        }

        return RedirectToAction("Index");
    }
}
