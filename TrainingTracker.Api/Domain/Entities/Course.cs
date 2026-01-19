using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingTracker.Api.Domain.Entities;

public class Course
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public int? ValidityMonths { get; set; }
    public string? Category { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    [NotMapped]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}