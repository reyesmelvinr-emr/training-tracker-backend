namespace TrainingTracker.Api.Infrastructure.Runtime;

/// <summary>
/// Captures process startup metadata for uptime calculations and future diagnostics.
/// </summary>
public sealed class StartupInfo
{
    public DateTime StartTimeUtc { get; } = DateTime.UtcNow;
}