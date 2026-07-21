using System.ComponentModel.DataAnnotations;
using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class CourseFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(32)]
    public string Code { get; set; } = "";

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    [StringLength(512)]
    public string? Tools { get; set; }

    public List<Guid> TeacherIds { get; set; } = [];

    public Guid? HeadTeacherId { get; set; }

    public string? DefaultChunkingStrategy { get; set; }
    public int? DefaultChunkSize { get; set; }
    public int? DefaultChunkOverlap { get; set; }
    public string? DefaultEmbeddingModel { get; set; }

    public IReadOnlyList<TeacherOptionDto> Teachers { get; set; } = [];

    public string? Error { get; set; }
}
