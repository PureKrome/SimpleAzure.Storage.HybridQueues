namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public sealed record HybridMessage<T>(T Content, string MessageId, string PopeReceipt, Guid? BlobId);
