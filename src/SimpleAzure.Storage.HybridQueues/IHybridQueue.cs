using Azure.Storage.Queues.Models;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public interface IHybridQueue
{
    /// <summary>
    /// Easy way to setup the required storage container/queue.
    /// </summary>
    /// <param name="isLoggingEnabled">Do we log any results?</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    Task SetupContainerStorageAsync(bool isLoggingEnabled, CancellationToken cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add an item to the queue and potentially the backing blob.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="item">An item to add to the queue.</param>
    /// <param name="initialVisibilityDelay">How long to initially hide the message.</param>
    /// <param name="isForcedOntoBlob">The item is placed on the blob regardless of the item's size.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If the item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessageAsync<T>(
        T item,
        TimeSpan? initialVisibilityDelay,
        bool isForcedOntoBlob,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to add a batch of messages to the queue and potentially the backing blob.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="contents">Collection of content to add to the queue.</param>
    /// <param name="initialVisibilityDelay">How long to initially hide the message.</param>
    /// <param name="batchSize">Number of messages per batch, to store as one parallel execution.</param>
    /// <param name="isForcedOntoBlob">The item is placed on the blob regardless of the item's size.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>If any item is a IsPrimitive (int, etc) or a string then it's stored -as is-. Otherwise, it is serialized to Json and then stored as Json.(</remarks>
    Task AddMessagesAsync<T>(
        IEnumerable<T> contents,
        TimeSpan? initialVisibilityDelay,
        int batchSize,
        bool isForcedOntoBlob,
        CancellationToken cancellationToken);

    /// <summary>
    /// Initiates an asynchronous operation to delete a message from the queue and if required, the linked blob.
    /// </summary>
    /// <param name="hybridMessage">Hybrid message which we will need to delete.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    Task DeleteMessageAsync<T>(HybridMessage<T> hybridMessage, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a message from a queue and potentially the backing blob. It will then wrap it in a simple Message class.
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
    /// <param name="messageCount">The number of messages to retrieve from the queue.</param>
    /// <param name="visibilityTimeout">A System.TimeSpan specifying the visibility timeout interval.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A System.Threading.Tasks.Task object that represents the asynchronous operation.</returns>
    /// <remarks>The content of the message will attempt to be deserialized from Json. If the message is a Primitive type or a string, then the Json deserialization will be still run but no error should occur.</remarks>
    Task<IReadOnlyList<HybridMessage<T>>> GetMessagesAsync<T>(
        int maxMessages,
        TimeSpan? visibilityTimeout,
        CancellationToken cancellationToken);

    /// <summary>
    /// Parses a QueueMessage into a HybridMessage with the smarts of retreiving the data from a blob, if required.
    /// </summary>
    /// <typeparam name="T">Type of item.</typeparam>
    /// <param name="queueMessage">Azure Storage QueueMessage.</param>
    /// <param name="cancellationToken">A System.Threading.CancellationToken to observe while waiting for a task to complete.</param>
    /// <returns>A HybridMessage with the relevant message content and extra Azure Storage Queue/Blob meta data.</returns>
    Task<HybridMessage<T>> ParseMessageAsync<T>(QueueMessage queueMessage, CancellationToken cancellationToken);
}
