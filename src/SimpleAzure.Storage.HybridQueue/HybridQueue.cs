using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public class HybridQueue : IHybridQueue

{
    private readonly QueueClient _queueClient;
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ILogger<HybridQueue> _logger;

    /// <inheritdoc />
    public HybridQueue(QueueClient queueClient, BlobContainerClient blobContainerClient, ILogger<HybridQueue> logger)
    {
        _queueClient = queueClient;
        _blobContainerClient = blobContainerClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task AddMessageAsync<T>(T item, TimeSpan? initialVisibilityDelay = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var _ = _logger.BeginCustomScope(("queueName", _queueClient.Name));

        _logger.LogDebug("Adding a message to a Hybrid Queue.");

        string message;

        // Don't waste effort serializing a string. It's already in a format that's ready to go.
        if (typeof(T).IsASimpleType())
        {
            if (item is string)
            {
                _logger.LogDebug("Item is a SimpleType: string.");

                message = item as string; // Note: shouldn't allocate new memory. Should just be a reference to existing memory.
            }
            else
            {
                _logger.LogDebug("Item is a SimpleType: something other than a string.");

                message = item.ToString();
            }
        }
        else
        {
            // It's a complex type, so serialize this as Json.

            _logger.LogDebug("Item is a ComplexType: {complexType}", item.GetType().ToString());

            message = JsonSerializer.Serialize(item);
        }

        // Is this item/content _too big_ for a normal queue-message?
        var messageSize = Encoding.UTF8.GetByteCount(message);
        if (messageSize > _queueClient.MessageMaxBytes)
        {
            // Yes - yes it is. Too big.
            // So lets store the content in Blob.
            // Then get the BlobId
            // Then store the BlobId GUID in the queue message.

            _logger.LogDebug("Item is too large to fit into a queue. Storing into a blob then a queue. Item size: {itemSize}", messageSize);

            var blobId = Guid.NewGuid().ToString(); // Unique Name/Identifier of this blob item.
            var binaryData = new BinaryData(message);
            await _blobContainerClient.UploadBlobAsync(blobId, binaryData, cancellationToken);

            message = blobId;

            _logger.LogDebug("Item added to blob. BlobId: {blobId}. Status: {responseStatus}", blobId);
        }

        await _queueClient.SendMessageAsync(
            message,
            initialVisibilityDelay,
            null,
            cancellationToken);

        _logger.LogDebug("Finished adding an Item to the queue.");
    }

    /// <inheritdoc />
    public async Task AddMessagesAsync<T>(
        IEnumerable<T> contents,
        TimeSpan? initialVisibilityDelay = null,
        int batchSize = 25,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contents);

        using var _ = _logger.BeginCustomScope(
            (nameof(batchSize), batchSize),
            ("queueName", _queueClient.Name));

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        // Lets batch up these messages to make sure the awaiting of all the tasks doesn't go too crazy.
        var contentsSize = contents.Count();
        var finalBatchSize = contentsSize > batchSize
                                 ? batchSize
                                 : contentsSize;

        foreach (var batch in contents.Chunk(finalBatchSize))
        {
            var tasks = batch.Select(content => AddMessageAsync(content, initialVisibilityDelay, cancellationToken));

            // Execute this batch.
            await Task.WhenAll(tasks);
        }
    }

    /// <inheritdoc />
    public async Task DeleteMessageAsync<T>(HybridMessage<T> hybridMessage, CancellationToken cancellationToken = default)
    {
        using var _ = _logger.BeginCustomScope(
            (nameof(hybridMessage.MessageId), hybridMessage.MessageId),
            (nameof(hybridMessage.PopeReceipt), hybridMessage.PopeReceipt),
            (nameof(hybridMessage.BlobId), hybridMessage.BlobId),
            ("queueName", _queueClient.Name));

        _logger.LogDebug("Deleting a message.");

        // We start with any blobs.
        if (hybridMessage.BlobId.HasValue)
        {
            _logger.LogDebug("Deleting message from Blob Container.");

            var blobClient = _blobContainerClient.GetBlobClient(hybridMessage.BlobId.Value.ToString());
            var blobResponse = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (!blobResponse.Value)
            {
                _logger.LogWarning("Failed to delete message from Blob Container.");
            }
        }

        var queueResponse = await _queueClient.DeleteMessageAsync(hybridMessage.MessageId, hybridMessage.PopeReceipt, cancellationToken);

        _logger.LogDebug("Deleted a message from the queue.");
    }

    /// <inheritdoc />
    public async Task<HybridMessage<T?>> GetMessageAsync<T>(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
    {
        var messages = await GetMessagesAsync<T>(1, visibilityTimeout, cancellationToken);

        if (messages?.Any() ?? false)
        {
            if (messages.Count() > 1)
            {
                throw new InvalidOperationException($"Expected 1 message but received {messages.Count()} messages");
            }

            return messages.First();
        }

        return default;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HybridMessage<T>>> GetMessagesAsync<T>(
        int maxMessages = 32,
        TimeSpan? visibilityTimeout = null,
        CancellationToken cancellationToken = default)
    {
        // Note: Why 32? That's the limit for Azure to pop at once.
        if (maxMessages < 1 ||
            maxMessages > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages));
        }

        using var _ = _logger.BeginCustomScope(
            (nameof(maxMessages), maxMessages),
            ("queueName", _queueClient.Name));

        _logger.LogDebug("About to receive queue message.");

        var response = await _queueClient.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);

        if (response == null ||
            response.Value == null)
        {
            _logger.LogDebug("Response was null or there were no Queue messages retrieved.");

            return Enumerable.Empty<HybridMessage<T>>();
        }

        _logger.LogDebug("Received {} messages from queue.", response.Value.Length);

        var hybridMessageTasks = response.Value.Select(x => ParseMessageAsync<T>(x, cancellationToken));

        var hybridMessages = await Task.WhenAll(hybridMessageTasks);

        return hybridMessages;
    }

    private async Task<HybridMessage<T>> ParseMessageAsync<T>(QueueMessage queueMessage, CancellationToken cancellationToken)
    {
        var message = queueMessage.Body?.ToString();

        if (message == null)
        {
            return new HybridMessage<T>(default, queueMessage.MessageId, queueMessage.PopReceipt, null);
        }

        if (Guid.TryParse(message, out var blobId))
        {
            using var _ = _logger.BeginCustomScope((nameof(blobId), blobId));

            _logger.LogDebug("Retreiving item via Blob Storage.");

            // Lets grab the item from the Blob.
            var blobClient = _blobContainerClient.GetBlobClient(blobId.ToString());
            using var stream = await blobClient.OpenReadAsync(null, cancellationToken);

            _logger.LogDebug("About to deserialize stream for a blob item from Blob Storage.");
            var blobItem = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken)!;
            _logger.LogDebug("Finished deserializing stream for a blob item from Blob Storage.");

            var hybridMessage = new HybridMessage<T>(blobItem, queueMessage.MessageId, queueMessage.PopReceipt, blobId);

            return hybridMessage;
        }
        else if (typeof(T).IsASimpleType())
        {
            _logger.LogDebug("Retreiving item: which is a simle type and not a guid/not in Blob Storage.");

            // Do we have a GUID? Guid's are used to represent the blobId.
            var value = (T)Convert.ChangeType(message, typeof(T));
            var hybridMessage = new HybridMessage<T>(value, queueMessage.MessageId, queueMessage.PopReceipt, null);

            return hybridMessage;
        }
        else
        {
            // Complex type, so lets assume it was serialized as Json ... so now we deserialize it.

            _logger.LogDebug("Retreiving a complex item: assumed as json so deserializing it.");

            var item = JsonSerializer.Deserialize<T>(message)!;

            var hybridMessage = new HybridMessage<T>(item, queueMessage.MessageId, queueMessage.PopReceipt, null);

            return hybridMessage;
        }
    }
}
