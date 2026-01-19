using System.ComponentModel.DataAnnotations;

namespace TrainingTracker.Api.Contracts.Courses;

public class CourseSummaryDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public int? ValidityMonths { get; init; }
    public string? Category { get; init; }
}

public class CourseDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public bool IsActive { get; init; }
    public int? ValidityMonths { get; init; }
    public string? Category { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedUtc { get; init; }
}

/// <summary>
/// Request DTO for creating a new course
/// </summary>
public class CreateCourseRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; } = true;

    [Range(1, 120, ErrorMessage = "Validity months must be between 1 and 120")]
    public int? ValidityMonths { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }
}

/// <summary>
/// Request DTO for updating an existing course
/// </summary>
public class UpdateCourseRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    public bool IsRequired { get; set; }

    public bool IsActive { get; set; }

    [Range(1, 120, ErrorMessage = "Validity months must be between 1 and 120")]
    public int? ValidityMonths { get; set; }

    [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
    public string? Category { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }
}

public sealed class PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}