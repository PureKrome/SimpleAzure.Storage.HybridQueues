namespace WorldDomination.SimpleAzure.Storage.HybridQueues;

public record class HybridMessage<T>(T? Content, string MessageId, string PopeReceipt, Guid? BlobId);
