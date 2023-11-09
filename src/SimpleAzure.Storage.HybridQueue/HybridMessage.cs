namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public record HybridMessage<T>(T Content, string MessageId, string PopeReceipt, Guid? BlobId);
