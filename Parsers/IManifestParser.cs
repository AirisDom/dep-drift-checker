using dep_drift_checker.Models;

namespace dep_drift_checker.Parsers;

public interface IManifestParser
{
    ManifestType ManifestType { get; }
    Task<IReadOnlyList<Dependency>> ParseAsync(string content);
}
