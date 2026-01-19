namespace TrainingTracker.Api.Contracts.Admin;

public record StatisticsResponse
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int InactiveUsers { get; init; }
    
    public int TotalCourses { get; init; }
    public int RequiredCourses { get; init; }
    public int OptionalCourses { get; init; }
    
    public int TotalEnrollments { get; init; }
    public int PendingEnrollments { get; init; }
    public int ActiveEnrollments { get; init; }
    public int CompletedEnrollments { get; init; }
    public int CancelledEnrollments { get; init; }
    
    public double CompletionRate { get; init; }
}

public record HealthResponse
{
    public string ApiStatus { get; set; } = "Unknown";
    public string DatabaseStatus { get; set; } = "Unknown";
    public string? DatabaseError { get; set; }
    public DateTime Timestamp { get; set; }
}

public record BulkUpdateUserStatusRequest
{
    public required List<Guid> UserIds { get; init; }
    public required bool IsActive { get; init; }
}

public record BulkUpdateResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
