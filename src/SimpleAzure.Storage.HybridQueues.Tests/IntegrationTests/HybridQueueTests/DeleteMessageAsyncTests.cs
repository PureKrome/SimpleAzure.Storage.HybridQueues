namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests;

public class DeleteMessageAsyncTests : CustomAzuriteTestContainer
{
    private readonly CancellationToken _cancellationToken = TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeleteMessageAsync_GivenALargeComplexInstance_ShouldDeleteTheBlobItemAndQueueMessage()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = new FakeMessage(QueueClient.MessageMaxBytes + 1);

        await HybridQueue.AddMessageAsync(message, _cancellationToken);
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        // Act.
        await HybridQueue.DeleteMessageAsync(retrievedMessage, _cancellationToken);

        // Assert.

        // Did the message actually get deleted?
        retrievedMessage.BlobId.ShouldNotBeNull();
        var blobClient = BlobContainerClient.GetBlobClient(retrievedMessage.BlobId.ToString());
        var existResponse = await blobClient.ExistsAsync(_cancellationToken);
        existResponse.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteMessageAsync_GivenTheBlobItemDoesntExist_ShouldDeleteQueueMessageAndLogAWarning()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = new FakeMessage(QueueClient.MessageMaxBytes + 1);

        await HybridQueue.AddMessageAsync(message, _cancellationToken);
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        // Now, manually delete this blob message (so we can test the warning).
        var existingBlobClient = BlobContainerClient.GetBlobClient(retrievedMessage.BlobId.ToString());
        await existingBlobClient.DeleteAsync(cancellationToken: _cancellationToken);

        // Act.
        await HybridQueue.DeleteMessageAsync(retrievedMessage, _cancellationToken);

        // Assert.
        var warningLog = Logger.Collector
            .GetSnapshot()
            .Single(x => x.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
        warningLog.Message.ShouldBe("Failed to delete message from Blob Container.");

        // Did the message actually get deleted?
        retrievedMessage.BlobId.ShouldNotBeNull();
        var blobClient = BlobContainerClient.GetBlobClient(retrievedMessage.BlobId.ToString());
        var existResponse = await blobClient.ExistsAsync(_cancellationToken);
        existResponse.Value.ShouldBeFalse();
    }
}
