using Microsoft.Extensions.Logging;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public static class LoggerExtensions
{
    public static IDisposable? BeginCustomScope<T>(this ILogger<T> logger, params (string Name, object? Value)[] scopeItems)
    {
        ArgumentNullException.ThrowIfNull(scopeItems);

        var scopeProps = new Dictionary<string, object?>();

        foreach (var (name, value) in scopeItems)
        {
            scopeProps[name] = value;
        }

        return logger.BeginScope(scopeProps);
    }

    public static IDisposable? BeginCustomScope(this ILogger logger, params (string Name, object? Value)[] scopeItems)
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
