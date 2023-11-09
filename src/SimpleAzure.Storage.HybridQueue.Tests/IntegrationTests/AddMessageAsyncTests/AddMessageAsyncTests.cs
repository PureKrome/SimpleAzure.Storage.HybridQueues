using Shouldly;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.AddMessageAsyncTests;

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

    [Fact]
    public async Task AddMessageAsync_GivenALargeComplexInstance_ShouldAddTheMessageToABlogAndThenAGuidToTheQueue()
    {
        // Arrange.
        var cancellationToken = CancellationToken.None;
        var message = new FakeMessage(QueueClient.MessageMaxBytes + 1);

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
