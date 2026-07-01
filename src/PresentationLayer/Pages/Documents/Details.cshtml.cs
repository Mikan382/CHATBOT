using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BusinessLayer.Services;
using DataAccessLayer.Entities;
using DataAccessLayer.Enums;

namespace PresentationLayer.Pages.Documents;

[Authorize(Roles = UserRoleNames.All)]
public class DetailsModel : PageModel
{
    private readonly DocumentService _documentService;

    public DetailsModel(DocumentService documentService)
    {
        _documentService = documentService;
    }

    public Document DocumentEntity { get; set; } = new();
    public bool CanManageDocuments { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            DocumentEntity = await _documentService.GetDetailsAsync(id, cancellationToken);
            CanManageDocuments = User.IsInRole(UserRoleNames.Teacher) || User.IsInRole(UserRoleNames.Admin);
            return Page();
        }
        catch
        {
            return NotFound();
        }
    }
}
