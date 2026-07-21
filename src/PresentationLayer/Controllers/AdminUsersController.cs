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
            input.Password = "";
            return View("Index", await BuildIndexModelAsync(
                null,
                null,
                cancellationToken,
                createUser: input,
                error: "Please check the user fields."));
        }

        try
        {
            await _userAdminService.CreateAsync(input.Email, input.FullName, input.Role, input.Password, cancellationToken);
            SetFlashSuccess("User was created.");
        }
        catch (InvalidOperationException ex)
        {
            input.Password = "";
            ModelState.Remove("CreateUser.Password");
            return View("Index", await BuildIndexModelAsync(
                null,
                null,
                cancellationToken,
                createUser: input,
                error: ex.Message));
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(UpdateUserInput input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildIndexModelAsync(
                null,
                null,
                cancellationToken,
                failedUpdate: input,
                error: "Please check the user fields."));
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
            return View("Index", await BuildIndexModelAsync(
                null,
                null,
                cancellationToken,
                failedUpdate: input,
                error: ex.Message));
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportCsv(IFormFile? csvFile, CancellationToken cancellationToken)
    {
        if (csvFile is null || csvFile.Length == 0)
        {
            SetFlashError("Please select a valid CSV file to import.");
            return RedirectToAction("Index");
        }

        var extension = Path.GetExtension(csvFile.FileName).ToLowerInvariant();
        if (extension != ".csv")
        {
            SetFlashError("Only .csv files are supported for batch student import.");
            return RedirectToAction("Index");
        }

        try
        {
            using var stream = csvFile.OpenReadStream();
            var result = await _userAdminService.ImportStudentsFromCsvAsync(stream, cancellationToken);
            if (result.SuccessCount > 0)
            {
                SetFlashSuccess($"Successfully imported {result.SuccessCount} student account(s) (Skipped {result.SkippedCount} existing). Default password: Student@123");
            }
            else if (result.SkippedCount > 0)
            {
                SetFlashError($"No new accounts created. {result.SkippedCount} account(s) already existed in the system.");
            }
            else
            {
                SetFlashError("Failed to import any student accounts. Please check your CSV file format.");
            }

            if (result.Errors.Count > 0)
            {
                SetFlashError($"Import warnings/errors: {string.Join(" | ", result.Errors.Take(3))}");
            }
        }
        catch (Exception ex)
        {
            SetFlashError($"Failed to process CSV file: {ex.Message}");
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
        CancellationToken cancellationToken,
        CreateUserInput? createUser = null,
        UpdateUserInput? failedUpdate = null,
        string? error = null)
    {
        return new UserAdminIndexViewModel
        {
            Users = await _userAdminService.ListAsync(searchTerm, role, cancellationToken),
            Roles = Roles,
            SearchTerm = searchTerm,
            SelectedRole = role,
            CurrentUserId = CurrentUserId(),
            CreateUser = createUser ?? new CreateUserInput(),
            FailedUpdate = failedUpdate,
            Error = error
        };
    }
}
