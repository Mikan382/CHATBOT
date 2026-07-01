using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Architecture;

[Authorize(Roles = UserRoleNames.TeacherOrAdmin)]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
