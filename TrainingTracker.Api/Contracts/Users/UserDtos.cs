using System.ComponentModel.DataAnnotations;

namespace TrainingTracker.Api.Contracts.Users;

public class UserSummaryDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
}

public class UserDetailDto : UserSummaryDto { }

/// <summary>
/// Request DTO for creating a new user
/// </summary>
public class CreateUserRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 128 characters")]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating an existing user
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(128, MinimumLength = 1, ErrorMessage = "Full name must be between 1 and 128 characters")]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}
