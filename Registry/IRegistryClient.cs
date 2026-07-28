namespace dep_drift_checker.Registry;

public interface IRegistryClient
{
    Task<string?> GetLatestVersionAsync(string packageName, CancellationToken cancellationToken = default);
}
