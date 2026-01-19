using System.ComponentModel.DataAnnotations;

namespace TrainingTracker.Api.Contracts.Enrollments;

public class EnrollmentSummaryDto
{
    public Guid Id { get; init; }
    public Guid CourseId { get; init; }
    public Guid UserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? EnrolledUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
}

public class EnrollmentDetailDto : EnrollmentSummaryDto { }

public class CreateEnrollmentRequest
{
    [Required(ErrorMessage = "CourseId is required")]
    public Guid CourseId { get; init; }

    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; init; }
}

public class UpdateEnrollmentStatusRequest
{
    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(PENDING|ACTIVE|COMPLETED|CANCELLED)$", ErrorMessage = "Invalid status. Must be PENDING, ACTIVE, COMPLETED, or CANCELLED")]
    public string Status { get; init; } = string.Empty;
}
