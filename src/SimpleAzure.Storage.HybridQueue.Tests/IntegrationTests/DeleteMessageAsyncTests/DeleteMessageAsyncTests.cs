using Shouldly;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.DeleteMessageAsyncTests;

public class DeleteMessageAsyncTests : CustomAzuriteTestContainer
{
    [Fact]
    public async Task DeleteMessageAsync_GivenALargeComplexInstance_ShouldDeleteTheBlobItemAndQueueMessage()
    {
        // Arrange.
        var cancellationToken = new CancellationToken();
        var message = new FakeMessage(QueueClient.MessageMaxBytes + 1);

        await HybridQueue.AddMessageAsync(message, default);
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();

        // Act.
        await HybridQueue.DeleteMessageAsync(retrievedMessage, default);

        // Assert.

        // Did the message actually get deleted?
        retrievedMessage.BlobId.ShouldNotBeNull();
        var blobClient = BlobContainerClient.GetBlobClient(retrievedMessage.BlobId.ToString());
        var existResponse = await blobClient.ExistsAsync(default);
        existResponse.Value.ShouldBeFalse();

    }
}
