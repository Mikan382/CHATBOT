using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using PresentationLayer.ViewModels;

namespace PresentationLayer.Pages.Account;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly AuthService _authService;

    public ChangePasswordModel(AuthService authService)
    {
        _authService = authService;
    }

    [BindProperty]
    public ChangePasswordViewModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var (success, error) = await _authService.ChangePasswordAsync(userId, Input.CurrentPassword, Input.NewPassword);
        if (!success)
        {
            Input.Error = error ?? "Password change failed.";
            return Page();
        }

        TempData["Success"] = "Password changed successfully.";
        return RedirectToPage();
    }
}
