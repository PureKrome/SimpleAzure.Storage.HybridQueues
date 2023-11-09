namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public interface IHybridQueue
{
    /// <summary>
    /// Initiates an asynchronous operation to add an item to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="item">An item to add to the queue.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If the item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessageAsync<T>(T item, CancellationToken cancellationToken) => AddMessageAsync(item, null, cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add an item to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="item">An item to add to the queue.</param>
    /// <param name="initialVisibilityDelay">How long to initially hide the message.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If the item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessageAsync<T>(
        T item,
        TimeSpan? initialVisibilityDelay,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add a batch messages to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="contents">Collection of content to add to the queue.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If any item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessagesAsync<T>(IEnumerable<T> contents, CancellationToken cancellationToken) =>
        AddMessagesAsync(contents, null, 25, cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add a batch messages to the queue.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="contents">Collection of content to add to the queue.</param>
    /// <param name="initialVisibilityDelay">How long to initially hide the message.</param>
    /// <param name="batchSize">Number of messages per batch, to store as one parallel execution.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If any item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessagesAsync<T>(
        IEnumerable<T> contents,
        TimeSpan? initialVisibilityDelay,
        int batchSize,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to delete a message from the queue and if required, the linked blob.
    /// </summary>
    /// <param name="hybridMessage">Hybrid message which we will need to delete.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    Task DeleteMessageAsync<T>(HybridMessage<T> hybridMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a message from a queue and wraps it in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    Task<HybridMessage<T?>> GetMessageAsync<T>(CancellationToken cancellationToken) => GetMessageAsync<T>(null, cancellationToken);

    /// <summary>
    /// Retrieves a message from a queue and wraps it in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="visibilityTimeout">A System.TimeSpan specifying the visibility timeout interval.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    Task<HybridMessage<T>?> GetMessageAsync<T>(TimeSpan? visibilityTimeout, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a collection of messages from a queue and wraps each one in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    Task<IEnumerable<HybridMessage<T>>> GetMessagesAsync<T>(CancellationToken cancellationToken) =>
        GetMessagesAsync<T>(32, null, cancellationToken);

    /// <summary>
    /// Retrieves a collection of messages from a queue and wraps each one in a simple Message class.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="messageCount">The number of messages to retrieve from the queue.</param>
    /// <param name="visibilityTimeout">A System.TimeSpan specifying the visibility timeout interval.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    Task<IEnumerable<HybridMessage<T>>> GetMessagesAsync<T>(
        int maxMessages,
        TimeSpan? visibilityTimeout,
        CancellationToken cancellationToken);
}
