using System.ComponentModel.DataAnnotations;
using BusinessLayer.DTOs;

namespace PresentationLayer.ViewModels;

public class BenchmarkDashboardViewModel
{
    public BenchmarkDashboardDto Dashboard { get; set; } = new(
        [],
        null,
        0,
        0,
        [],
        [],
        false,
        false,
        [],
        []);
    public BenchmarkRunInput Input { get; set; } = new();
    public string? Error { get; set; }
}

public class BenchmarkRunInput
{
    [Required]
    public Guid? CourseId { get; set; }

    [Required]
    [StringLength(64)]
    public string ChunkingStrategy { get; set; } = "";

    [Required]
    [StringLength(160)]
    public string EmbeddingModel { get; set; } = "";

    [Range(1, 10)]
    public int TopK { get; set; } = 3;
}

public class BenchmarkQuestionPageViewModel
{
    public BenchmarkQuestionPageDto Page { get; set; } = new([], [], null, null, null);
}

public class BenchmarkQuestionFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid? CourseId { get; set; }

    [Required]
    public Guid? ExpectedDocumentId { get; set; }

    [Required]
    [StringLength(2000)]
    public string Question { get; set; } = "";

    [Required]
    [StringLength(8000)]
    public string ExpectedAnswer { get; set; } = "";

    [Range(1, 10000)]
    public int DisplayOrder { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public IReadOnlyList<BenchmarkCourseOptionDto> Courses { get; set; } = [];

    public IReadOnlyList<BenchmarkDocumentOptionDto> Documents { get; set; } = [];

    public string? Error { get; set; }
}

public class BenchmarkRunDetailsViewModel
{
    public BenchmarkRunDetailsDto Details { get; set; } = null!;
}
