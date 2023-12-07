using Microsoft.Extensions.Logging;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

internal static class LoggerExtensions
{
    internal static IDisposable? BeginCustomScope(this ILogger logger, params (string Name, object? Value)[] scopeItems)
    {
        ArgumentNullException.ThrowIfNull(scopeItems);

        var scopeProps = new Dictionary<string, object?>();

        foreach (var (name, value) in scopeItems)
        {
            scopeProps[name] = value;
        }

        return logger.BeginScope(scopeProps);
    }
}
