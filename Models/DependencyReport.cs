namespace dep_drift_checker.Models;

public class DependencyReport
{
    public List<Dependency> Dependencies { get; set; } = new();
    public double StalenessScore { get; set; }
    public DateTime AnalyzedAt { get; set; }
}
