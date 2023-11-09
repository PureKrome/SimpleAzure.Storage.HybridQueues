using Shouldly;

namespace WorldDomination.SimpleAzure.Storage.HybridQueues.Tests.IntegrationTests.AddMessageAsyncTests;

public class AddMessageAsyncTests : CustomAzuriteTestContainer
{
    [Fact]
    public async Task AddMessageAsync_GivenSomeSimpleString_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var message = "hello";

        // Act.
        await HybridQueue.AddMessageAsync(message, default);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<string>();
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldBe(message);
    }

    [Fact]
    public async Task AddMessageAsync_GivenASimpleInt_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var message = 1234;

        // Act.
        await HybridQueue.AddMessageAsync(message, default);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<int>();
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldBe(message);
    }

    [Fact]
    public async Task AddMessageAsync_GivenAComplexInstance_ShouldAddTheMessageToTheQueue()
    {
        // Arrange.
        var message = new FakeMessage(10);

        // Act.
        await HybridQueue.AddMessageAsync(message, default);

        // Assert.
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>();
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.Content.ShouldNotBeNull();
        retrievedMessage.Content.Content.ShouldBe(message.Content);
    }

    [Fact]
    public async Task AddMessageAsync_GivenALargeComplexInstance_ShouldAddTheMessageToABlogAndThenAGuidToTheQueue()
    {
        // Arrange
        var message = new FakeMessage(QueueClient.MessageMaxBytes + 1);

        // Act
        await HybridQueue.AddMessageAsync(message, default);

        // Assert
        var retrievedMessage = await HybridQueue.GetMessageAsync<FakeMessage>();
        retrievedMessage.ShouldNotBeNull();
        retrievedMessage.BlobId.ShouldNotBeNull();
        retrievedMessage.Content.ShouldNotBeNull();
        retrievedMessage.Content.Content.ShouldBe(message.Content);
    }
}
