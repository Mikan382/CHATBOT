using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Enums;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.AdminUsers;

[Authorize(Roles = UserRoleNames.Admin)]
public class IndexModel : PageModel
{
    private readonly UserAdminService _userAdminService;

    public IndexModel(UserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    public IReadOnlyList<UserListDto> Users { get; set; } = [];
    public IReadOnlyList<string> Roles { get; set; } = [UserRoleNames.Student, UserRoleNames.Teacher, UserRoleNames.Admin];

    [BindProperty]
    public CreateUserInput CreateUser { get; set; } = new();

    public async Task OnGetAsync()
    {
        Users = await _userAdminService.ListAsync();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostCreateAsync()
    {
        if (!ModelState.IsValid)
        {
            Users = await _userAdminService.ListAsync();
            return Page();
        }

        try
        {
            await _userAdminService.CreateAsync(
                CreateUser.Email,
                CreateUser.FullName,
                CreateUser.Role,
                CreateUser.Password);
            TempData["Success"] = "User was created.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostChangeRoleAsync(Guid userId, string role)
    {
        try
        {
            await _userAdminService.ChangeRoleAsync(userId, role);
            TempData["Success"] = "User role was updated.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostLockAsync(Guid userId)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(userId, true);
            TempData["Success"] = "User was locked.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostUnlockAsync(Guid userId)
    {
        try
        {
            await _userAdminService.SetLockoutAsync(userId, false);
            TempData["Success"] = "User was unlocked.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostDeleteAsync(Guid userId)
    {
        try
        {
            await _userAdminService.DeleteAsync(userId);
            TempData["Success"] = "User was deleted.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostResetPasswordAsync(Guid userId, string newPassword)
    {
        try
        {
            await _userAdminService.ResetPasswordAsync(userId, newPassword);
            TempData["Success"] = "Password was reset.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToPage();
    }
}
