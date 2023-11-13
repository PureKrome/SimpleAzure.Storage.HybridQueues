namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public static class IHybridQueueExtensions
{
    /// <summary>
    /// Initiates an asynchronous operation to add an item to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="queue">The queue to operate on</param>
    /// <param name="item">An item to add to the queue.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If the item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    public static Task AddMessageAsync<T>(this IHybridQueue queue, T item, CancellationToken cancellationToken) =>
        queue.AddMessageAsync(item, null, cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add a batch messages to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="queue">The queue to operate on</param>
    /// <param name="contents">Collection of content to add to the queue.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If any item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    public static Task AddMessagesAsync<T>(this IHybridQueue queue, IEnumerable<T> contents, CancellationToken cancellationToken) =>
        queue.AddMessagesAsync(contents, null, 25, cancellationToken);

    /// <summary>
    /// Retrieves a message from a queue and wraps it in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="queue">The queue to operate on</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    public static Task<HybridMessage<T>?> GetMessageAsync<T>(this IHybridQueue queue, CancellationToken cancellationToken) =>
        queue.GetMessageAsync<T>(null, cancellationToken);

    /// <summary>
    /// Retrieves a collection of messages from a queue and wraps each one in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="queue">The queue to operate on</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    public static Task<HybridMessage<T>[]> GetMessagesAsync<T>(this IHybridQueue queue, CancellationToken cancellationToken) =>
        queue.GetMessagesAsync<T>(32, null, cancellationToken);
}
