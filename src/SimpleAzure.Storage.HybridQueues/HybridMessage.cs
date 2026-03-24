namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public sealed record HybridMessage<T>(T Content, string MessageId, string PopReceipt, Guid? BlobId)
{
    /// <summary>
    /// Gets or initializes the pop receipt using the legacy property name.
    /// </summary>
    /// <remarks>
    /// This property is maintained for backward compatibility. Use <see cref="PopReceipt"/> instead.
    /// </remarks>
    [Obsolete("Use PopReceipt instead. This alias will be removed in a future major version.")]
    public string PopeReceipt
    {
        get => PopReceipt;
        init => PopReceipt = value;
    }
}
