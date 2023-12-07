using Azure.Storage.Queues;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.GetMessagesAsyncTests
{
    public class GetMessagesAsyncTests : CustomAzuriteTestContainer
    {
        private const string ValidNullJson = "null";

        [Theory]
        [InlineData(0)]
        [InlineData(33)]
        public async Task GetMessagesAsync_GivenAnInvalidMaxMessages_ShouldThrowAnException(int maxMessages)
        {
            // Arrange.
            var cancellationToken = CancellationToken.None;

            // Act.
            var exception = await Should.ThrowAsync<ArgumentOutOfRangeException>(HybridQueue.GetMessagesAsync<string>(
                maxMessages,
                null,
                cancellationToken));

            // Assert.
            exception.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetMessagesAsync_GivenNoMessageExists_ShouldReturnAnEmptyCollectoin()
        {
            // Arrange.
            var cancellationToken = CancellationToken.None;

            // Act.
            var messages = await HybridQueue.GetMessagesAsync<string>(
                10,
                null,
                cancellationToken);

            // Assert.
            messages.ShouldBeEmpty();
        }

        // We're checking that the message on the blob can be deserialzied ok.
        // Maybe someone overwrote the original blob?
        // Maybe someone manally placed a bad queue message which references the wrong/bad blob?
        // Sure, this is a rare edge case.
        [Fact]
        public async Task GetMessagesAsync_GivenTheBlobItemCannotBeDeserialized_ShouldThrowAnException()
        {
            // Arrange.
            var visibilityTimeout = TimeSpan.FromSeconds(1);
            var cancellationToken = CancellationToken.None;
            var message = new FakeMessage();

            await HybridQueue.AddMessageAsync(message, null, true, cancellationToken); // Force the message onto the blob.
            var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(visibilityTimeout, cancellationToken); // Message is available asap.
            retrievedMessage.ShouldNotBeNull();

            // Wait a bit for the message to be available on the blob.
            // Otherwise the next call is too quick and the blob isn't available yet.
            await Task.Delay(visibilityTimeout, cancellationToken);

            // Now we expect a "FakeMessage" type on the blob item, so lets hack the blob to be anything but that.
            // We do this by deleting the existing one and uploading a new one.
            var existingBlobClient = BlobContainerClient.GetBlobClient(retrievedMessage.BlobId.ToString());
            var content = new BinaryData(ValidNullJson); // Bad Data - not a FakeMessage. This is valid JSON which is deserialized to null 🙃
            await existingBlobClient.UploadAsync(content, true, cancellationToken);

            // Act.
            var exception = await Should.ThrowAsync<InvalidOperationException>(HybridQueue.GetMessageAsync<string>(cancellationToken));

            // Assert.
            exception.ShouldNotBeNull();
        }

        [Fact]
        public async Task GetMessagesAsync_GivenTheQueueItemCannotBeDeserialized_ShouldThrowAnException()
        {
            // Arrange.
            var cancellationToken = CancellationToken.None;

            // Hack the message queue by manually placing a "bad" json payload onto the queue.
            await QueueClient.SendMessageAsync(
                ValidNullJson,
                null,
                null,
                cancellationToken);

            // Act.
            var exception = await Should.ThrowAsync<InvalidOperationException>(HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken));

            // Assert.
            exception.ShouldNotBeNull();
        }
    }
}
