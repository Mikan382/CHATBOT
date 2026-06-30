using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BusinessLayer.Services;
using DataAccessLayer.Enums;

namespace PresentationLayer.ApiControllers;

[ApiController]
[Authorize(Roles = UserRoleNames.All)]
public class DocumentsApiController : ControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentsApiController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet("/api/documents")]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var documents = await _documentService.ListDocumentsAsync(cancellationToken);
        return Ok(new { success = true, documents });
    }

    [HttpGet("/api/documents/{id:guid}/chunks")]
    public async Task<IActionResult> Chunks(Guid id, CancellationToken cancellationToken)
    {
        var chunks = await _documentService.ListChunksAsync(id, cancellationToken);
        return Ok(new { success = true, chunks });
    }
}
