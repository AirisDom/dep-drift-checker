namespace dep_drift_checker.Models;

public class Dependency
{
    public required string Name { get; set; }
    public required string CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public int? DaysBehind { get; set; }
    public DependencySeverity Severity { get; set; }
}

public enum DependencySeverity
{
    UpToDate,
    Low,
    Medium,
    High,
    Critical
}
