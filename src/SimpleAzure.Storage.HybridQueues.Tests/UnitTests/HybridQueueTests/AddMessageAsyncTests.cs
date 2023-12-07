using Microsoft.Extensions.Logging;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.UnitTests.HybridQueueTests;

public class AddMessageAsyncTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task AddMessageAsync_GivenABadBatchSize_ShouldThrowAnException(int batchSize)
    {
        // Arrange.
        var contents = new List<string> { "message1", "message2", "message3" };
        var queueClient = new Mock<QueueClient>();
        var blobContainerClient = new Mock<BlobContainerClient>();
        var logger = new Mock<ILogger<HybridQueue>>();

        var hybridQueue = new HybridQueue(queueClient.Object, blobContainerClient.Object, logger.Object);

        // Act.
        var exception = await Should.ThrowAsync<ArgumentOutOfRangeException>(
            hybridQueue.AddMessagesAsync(
                contents,
                null,
                batchSize,
                false,
                CancellationToken.None));

        // Assert.
        exception.ShouldNotBeNull();
        queueClient.Verify(x => x.SendMessageAsync(
            It.IsAny<string>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
