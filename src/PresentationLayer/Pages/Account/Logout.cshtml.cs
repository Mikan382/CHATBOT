using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;

namespace PresentationLayer.Pages.Account;

[Authorize]
public class LogoutModel : PageModel
{
    private readonly AuthService _authService;

    public LogoutModel(AuthService authService)
    {
        _authService = authService;
    }

    public IActionResult OnGet()
    {
        return RedirectToPage("/Account/Login");
    }

    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OnPostAsync()
    {
        await _authService.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }
}
