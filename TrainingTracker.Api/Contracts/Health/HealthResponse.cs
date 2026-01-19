namespace TrainingTracker.Api.Contracts.Health;

public sealed class HealthResponse
{
    public required string Status { get; init; }
    public required string Version { get; init; }
    public DateTime TimestampUtc { get; init; }
    public long UptimeSeconds { get; init; }
}