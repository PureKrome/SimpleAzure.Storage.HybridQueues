namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public static class IHybridQueueExtensions
{
    /// <inheritdoc cref="IHybridQueue.AddMessageAsync{T}(T, TimeSpan?, CancellationToken)" />
    public static Task AddMessageAsync<T>(this IHybridQueue queue, T item, CancellationToken cancellationToken) =>
        queue.AddMessageAsync(item, null, cancellationToken);

    /// <inheritdoc cref="IHybridQueue.AddMessagesAsync{T}(IEnumerable{T}, TimeSpan?, int, CancellationToken)" />
    public static Task AddMessagesAsync<T>(this IHybridQueue queue, IEnumerable<T> contents, CancellationToken cancellationToken) =>
        queue.AddMessagesAsync(contents, null, 25, cancellationToken);

    /// <inheritdoc cref="IHybridQueue.GetMessageAsync{T}(TimeSpan?, CancellationToken)" />
    public static Task<HybridMessage<T>?> GetMessageAsync<T>(this IHybridQueue queue, CancellationToken cancellationToken) =>
        queue.GetMessageAsync<T>(null, cancellationToken);

    /// <inheritdoc cref="IHybridQueue.GetMessagesAsync{T}(int, TimeSpan?, CancellationToken)" />
    public static Task<HybridMessage<T>[]> GetMessagesAsync<T>(this IHybridQueue queue, CancellationToken cancellationToken) =>
        queue.GetMessagesAsync<T>(32, null, cancellationToken);
}
