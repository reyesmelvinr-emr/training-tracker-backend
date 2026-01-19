using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingTracker.Api.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    
    [NotMapped]
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}