using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingTracker.Api.Domain.Entities;

public class Enrollment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CourseId { get; set; }
    public Guid UserId { get; set; }
    
    // EnrolledUtc and CompletedUtc are not in SSDT schema; mark NotMapped to prevent EF from querying them
    [NotMapped]
    public DateTime? EnrolledUtc { get; set; }
    
    [NotMapped]
    public DateTime? CompletedUtc { get; set; }
    
    // Default status aligns with SSDT default
    public string Status { get; set; } = "PENDING";
}