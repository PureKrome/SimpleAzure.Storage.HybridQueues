namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.HybridQueueTests;

public class AddMessageAsyncTests : CustomAzuriteTestContainer
{
    [Fact]
    public async Task AddMessageAsync_GivenSomeSimpleString_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = "hello";

        // Act.
        await HybridQueue.AddMessageAsync(message, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<string>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldBe(message);
    }

    [Fact]
    public async Task AddMessageAsync_GivenSomeSimpleStringAndFocedOntoTheBlob_ShouldAddTheMessageToTheBlobAndQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = "hello";

        // Act.
        await HybridQueue.AddMessageAsync(message, null, true, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<string>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldBe(message);
    }

    [Fact]
    public async Task AddMessageAsync_GivenASimpleInt_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = 1234;

        // Act.
        await HybridQueue.AddMessageAsync(message, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<int>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldBe(message);
    }

    [Fact]
    public async Task AddMessageAsync_GivenAComplexInstance_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = new FakeMessage(10);

        // Act.
        await HybridQueue.AddMessageAsync(message, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldNotBeNull();
        retrievedMessage.Content.Content.ShouldBe(message.Content);
    }

    [Theory]
    [InlineData(65536 + 1)] // 64kb which is the max for Plain Text encoded queues.
    [InlineData(49152 + 1)] // 48kb which is the max for Base64 encoded queues.
    public async Task AddMessageAsync_GivenALargeComplexInstance_ShouldAddTheMessageToABlogAndThenAGuidToTheQueue(int messageMaxBytes)
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = new FakeMessage(messageMaxBytes);

        // Act.
        await HybridQueue.AddMessageAsync(message, cancellationToken);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>(cancellationToken);
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.BlobId.ShouldNotBeNull();
        retrievedMessage.Content.ShouldNotBeNull();
        retrievedMessage.Content.Content.ShouldBe(message.Content);
    }
}
