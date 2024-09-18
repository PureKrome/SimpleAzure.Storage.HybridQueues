using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public sealed class HybridQueue(
    QueueClient queueClient,
    BlobContainerClient blobContainerClient,saddafsa
    ILogger<HybridQueue> logger) : IHybridQueue
{
    private const int MessageSize = 49152; // 48KB as bytes. The message size for plain text is 64KB and Base64 is 48KB. We can't
                                           // access the MessageEncoding value so we have to be conservative and stick with the lower value -> 48KB

    private readonly QueueClient _queueClient = queueClient;
    private readonly BlobContainerClient _blobContainerClient = blobContainerClient;
    private readonly ILogger<HybridQueue> _logger = logger;

    private static readonly BlobHttpHeaders _httpHeaders = new()
    {
        ContentType = "application/json",
    };

    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task SetupContainerStorageAsync(bool isLoggingEnabled, CancellationToken cancellationToken)
    {
        var blobAzureResponse = await _blobContainerClient
            .CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (isLoggingEnabled)
        {
            if (blobAzureResponse?.Value != null)
            {
                _logger.LogInformation("❌ Missing Hybrid storage container - creating a new one.");
            }
        else
            {
                _logger.LogDebug("✅ Hybrid storage container already exists.");
                
            }
        }

        var queueAzureResponse = await _queueClient
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (isLoggingEnabled)
        {
            if (queueAzureResponse != null)
            {
                _logger.LogInformation("❌ Missing Hybrid storage queue - creating a new one.");
            }
            else
            {
                _logger.LogDebug("✅ Hybrid storage queue already exists.");
            }
        }
    }

    public async Task AddMessageAsync<T>(
        T item,
        TimeSpan? initialVisibilityDelay,
        bool isForcedOntoBlob,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var _ = _logger.BeginCustomScope(
            ("queueName", _queueClient.Name),
            (nameof(isForcedOntoBlob), isForcedOntoBlob));

        _logger.LogDebug("Adding a message to a Hybrid Queue.");

        string message;


        // Don't waste effort serializing a string. It's already in a format that's ready to go.
        if (!isForcedOntoBlob &&
            item is string stringItem)
        {
            _logger.LogDebug("Item is a SimpleType: string.");
            message = stringItem;
        }
        else if (!isForcedOntoBlob &&
            typeof(T).IsASimpleType())
        {
            _logger.LogDebug("Item is a SimpleType: something other than a string.");

            // IsASimpleType ensures that item is a primitive type, or decimal, and none of them
            // return null from their ToString method.
            message = item.ToString().AssumeNotNull();
        }
        else
        {
            if (isForcedOntoBlob)
            {
                _logger.LogDebug("Is forced onto blob == true.");
            }
            else
            {
                _logger.LogDebug("Item is a ComplexType: {complexType}", item.GetType().ToString());
            }

            message = JsonSerializer.Serialize(item);
        }

        // Is this item/content _too big_ for a normal queue-message?
        var messageSize = isForcedOntoBlob
            ? -1 // Don't need to determine the byte count because we 
            : Encoding.UTF8.GetByteCount(message);

        using var __ = _logger.BeginCustomScope((nameof(messageSize), messageSize));

        if (isForcedOntoBlob || messageSize > MessageSize)
        {
            if (!isForcedOntoBlob)
            {
               _logger.LogDebug("Item is too large to fit into a queue. Storing into a blob then a queue. Item size: {itemSize:N0} bytes", messageSize);
            }

            message = await AddJsonMessageToBlobStorageAsync(message, messageSize, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            await _queueClient.SendMessageAsync(
                message,
                initialVisibilityDelay,
                null,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to add an Item to the queue. Error: {errorMessage}", exception.Message);
            throw;
        }

        _logger.LogDebug("Finished adding an Item to the queue.");
    }

    public async Task AddMessagesAsync<T>(
        IEnumerable<T> contents,
        TimeSpan? initialVisibilityDelay,
        int batchSize,
        bool isForcedOntoBlob,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(batchSize);

        using var _ = _logger.BeginCustomScope(
            (nameof(batchSize), batchSize),
            ("queueName", _queueClient.Name));

        // Lets batch up these messages to make sure the awaiting of all the tasks doesn't go too crazy.
        foreach (var batch in contents.Chunk(batchSize))
        {
            var tasks = batch.Select(content => AddMessageAsync(content, initialVisibilityDelay, isForcedOntoBlob, cancellationToken));

            // Execute this batch.
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    public async Task DeleteMessageAsync<T>(HybridMessage<T> hybridMessage, CancellationToken cancellationToken)
    {
        using var _ = _logger.BeginCustomScope(
            (nameof(hybridMessage.MessageId), hybridMessage.MessageId),
            (nameof(hybridMessage.PopeReceipt), hybridMessage.PopeReceipt),
            (nameof(hybridMessage.BlobId), hybridMessage.BlobId),
            ("queueName", _queueClient.Name));

        _logger.LogDebug("Deleting a message.");

        // We start with any blobs.
        if (hybridMessage.BlobId is { } blobId)
        {
            _logger.LogDebug("Deleting message from Blob Container.");

            var blobClient = _blobContainerClient.GetBlobClient(blobId.ToString());
            var blobResponse = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (!blobResponse.Value)
            {
                _logger.LogWarning("Failed to delete message from Blob Container.");
            }
        }

        var queueResponse = await _queueClient
            .DeleteMessageAsync(hybridMessage.MessageId, hybridMessage.PopeReceipt, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Deleted a message from the queue.");
    }

    public async Task<HybridMessage<T>?> GetMessageAsync<T>(TimeSpan? visibilityTimeout, CancellationToken cancellationToken)
    {
        var messages = await GetMessagesAsync<T>(1, visibilityTimeout, cancellationToken).ConfigureAwait(false);

        var result = messages switch
        {
            [] => null,
            [{ } first] => first,
            _ => throw new InvalidOperationException($"Expected 1 message but received {messages.Count} messages")
        };
        return result;
    }

    public async Task<IReadOnlyList<HybridMessage<T>>> GetMessagesAsync<T>(
        int maxMessages,
        TimeSpan? visibilityTimeout,
        CancellationToken cancellationToken)
    {
        // Note: Why 32? That's the limit for Azure to pop at once.
        if (maxMessages is < 1 or > 32)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMessages));
        }

        using var _ = _logger.BeginCustomScope(
            (nameof(maxMessages), maxMessages),
            ("queueName", _queueClient.Name));

        _logger.LogDebug("About to receive queue message.");

        var response = await _queueClient.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken).ConfigureAwait(false);
        var messages = response.Value;

        if (messages.Length <= 0)
        {
            _logger.LogDebug("No Queue messages retrieved.");
            return Array.Empty<HybridMessage<T>>();
        }

        _logger.LogDebug("Received {messageCount} messages from queue.", messages.Length);

        var hybridMessageTasks = messages.Select(x => ParseMessageAsync<T>(x, cancellationToken));

        var hybridMessages = await Task.WhenAll(hybridMessageTasks).ConfigureAwait(false);
        return hybridMessages;
    }

    public async Task<HybridMessage<T>> ParseMessageAsync<T>(QueueMessage queueMessage, CancellationToken cancellationToken)
    {
        var message = queueMessage.Body.ToString().AssumeNotNull();

        // Queue Message: Guid == item is in Blob Storage.
        // Blob Storage: Complex Type. Json message.
        if (Guid.TryParse(message, out var blobId))
        {
            using var _ = _logger.BeginCustomScope((nameof(blobId), blobId));

            _logger.LogDebug("Retrieving item via Blob Storage.");

            // Lets grab the item from the Blob.
            var blobClient = _blobContainerClient.GetBlobClient(blobId.ToString());
            using var stream = await blobClient.OpenReadAsync(null, cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("About to deserialize stream for a blob item from Blob Storage.");

            var blobItem = await JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Finished deserializing stream for a blob item from Blob Storage.");

            if (blobItem is null)
            {
                throw new InvalidOperationException($"Could not deserialize blob '{blobId}' for message '{queueMessage.MessageId}'.");
            }

            var hybridMessage = new HybridMessage<T>(blobItem, queueMessage.MessageId, queueMessage.PopReceipt, blobId);
            return hybridMessage;
        }

        // Queue Message: simple type. Not JSON.
        // Blob Storage: N/A
        else if (typeof(T).IsASimpleType())
        {
            _logger.LogDebug("Retrieving item: which is a simle type and not a guid/not in Blob Storage.");

            // Do we have a GUID? Guids are used to represent the blobId.
            var value = (T)Convert.ChangeType(message, typeof(T));

            var hybridMessage = new HybridMessage<T>(value, queueMessage.MessageId, queueMessage.PopReceipt, null);
            return hybridMessage;
        }

        // Queue Message: complex type. Json.
        // Blob Storage: N/A
        else
        {
            // Complex type, so lets assume it was serialized as Json ... so now we deserialize it.
            _logger.LogDebug("Retrieving a complex item: assumed as json so deserializing it.");

            var item = JsonSerializer.Deserialize<T>(message, _jsonSerializerOptions);
            if (item is null)
            {
                throw new InvalidOperationException($"Could not deserialize complex type for message '{queueMessage.MessageId}'.");
            }

            return new HybridMessage<T>(item, queueMessage.MessageId, queueMessage.PopReceipt, null);
        }
    }

    private async Task<string> AddJsonMessageToBlobStorageAsync(string jsonMessage, int messageSize, CancellationToken cancellationToken)
    {
        // Yes - yes it is. Too big.
        // So lets store the content in Blob.
        // Then get the BlobId
        // Then store the BlobId GUID in the queue message.

        var blobId = Guid.NewGuid().ToString(); // Unique Name/Identifier of this blob item.
        var content = new BinaryData(jsonMessage);

        var blobClient = _blobContainerClient.GetBlobClient(blobId);
        await blobClient
            .UploadAsync(content, new BlobUploadOptions { HttpHeaders = _httpHeaders }, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Item added to blob. BlobId: {blobId}.", blobId);
        return blobId;
    }
}
